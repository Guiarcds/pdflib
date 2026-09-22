namespace OrganizadorDocumentos.Core.Services;

using OrganizadorDocumentos.Core.Enums;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class ProcessamentoService : IProcessamentoService
{
    private readonly IApiService _apiService;
    private readonly IFileService _fileService;
    private readonly IMapeamentoService _mapeamento;
    private readonly INormalizacaoService _normalizacao;
    private readonly IConfiguracaoService _configuracao;
    private readonly ILogService _log;

    public event EventHandler<ResultadoProcessamento>? DocumentoProcessado;

    public ProcessamentoService(
        IApiService apiService,
        IFileService fileService,
        IMapeamentoService mapeamento,
        INormalizacaoService normalizacao,
        IConfiguracaoService configuracao,
        ILogService log)
    {
        _apiService = apiService;
        _fileService = fileService;
        _mapeamento = mapeamento;
        _normalizacao = normalizacao;
        _configuracao = configuracao;
        _log = log;
    }

    public async Task<List<ResultadoProcessamento>> ProcessarPdfCompletoAsync(string caminhoPdf)
    {
        var resultados = new List<ResultadoProcessamento>();
        string? pastaTemp = null;

        try
        {
            _log.Informacao($"Processando (novo fluxo 4 fases): {Path.GetFileName(caminhoPdf)}");

            var config = _configuracao.ObterConfiguracao();
            pastaTemp = Path.Combine(config.PastaRaiz, "TEMP_PROCESSAMENTO", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pastaTemp);

            var decisoes = await _apiService.AnalisarEstruturaPdfAsync(caminhoPdf);
            _log.Informacao($"Fase 1 concluída: {decisoes.Count} decisão(ões) recebidas, {decisoes.Count(d => d.Action == PageAction.Discard)} página(s) descartada(s)");

            if (decisoes.Count == 0)
            {
                resultados.Add(new ResultadoProcessamento
                {
                    ArquivoOrigem = caminhoPdf,
                    Status = StatusProcessamento.Revisar,
                    Mensagem = "IA não retornou decisões para nenhuma página"
                });
                return resultados;
            }

            var imagensCorrigidas = await _fileService.AplicarCorrecoesAsync(caminhoPdf, decisoes, pastaTemp);

            if (imagensCorrigidas.Count == 0)
            {
                resultados.Add(new ResultadoProcessamento
                {
                    ArquivoOrigem = caminhoPdf,
                    Status = StatusProcessamento.Revisar,
                    Mensagem = "Todas as páginas foram descartadas (em branco/ruído)"
                });
                return resultados;
            }

            var pdfsIndividuais = await _fileService.SplitUmaPaginaPorPdfAsync(imagensCorrigidas, pastaTemp);

            _log.Informacao($"Fase 4: extraindo dados de {pdfsIndividuais.Count} documento(s) individual(is)");

            foreach (var pdf in pdfsIndividuais)
            {
                var dados = await _apiService.ExtrairDadosDocumentoAsync(pdf);
                var resultado = await ProcessarDocumentoIndividualAsync(pdf, dados);
                resultados.Add(resultado);
                DocumentoProcessado?.Invoke(this, resultado);
            }

            LimparTemp(pastaTemp);
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro no processamento completo: {caminhoPdf}", ex);
            resultados.Add(new ResultadoProcessamento
            {
                ArquivoOrigem = caminhoPdf,
                Status = StatusProcessamento.Erro,
                Mensagem = $"Erro geral: {ex.Message}"
            });

            if (pastaTemp != null)
                LimparTemp(pastaTemp);
        }

        return resultados;
    }

    public async Task<ResultadoProcessamento> ProcessarDocumentoAsync(string caminhoPdf)
    {
        var resultados = await ProcessarPdfCompletoAsync(caminhoPdf);

        var principal = resultados.FirstOrDefault() ?? new ResultadoProcessamento
        {
            ArquivoOrigem = caminhoPdf,
            Status = StatusProcessamento.Erro,
            Mensagem = "Nenhum documento extraído"
        };

        foreach (var r in resultados)
        {
            DocumentoProcessado?.Invoke(this, r);
        }

        return principal;
    }

    private async Task<ResultadoProcessamento> ProcessarDocumentoIndividualAsync(string caminhoPdf, DocumentoFinanceiro dados)
    {
        const double LIMIAR_CONFIANCA = 0.70;

        var resultado = new ResultadoProcessamento
        {
            ArquivoOrigem = caminhoPdf,
            DadosExtraidos = dados,
            Status = StatusProcessamento.Erro
        };

        try
        {
            if (!dados.TemColaborador || dados.Confianca < LIMIAR_CONFIANCA)
            {
                resultado.Status = StatusProcessamento.Revisar;
                resultado.Mensagem = !dados.TemColaborador
                    ? "Colaborador não identificado no documento"
                    : $"Colaborador identificado com baixa confiança ({dados.Confianca:P0} < {LIMIAR_CONFIANCA:P0})";
                _log.Aviso(resultado.Mensagem);
            }
            else if (!dados.TemCompetencia)
            {
                resultado.Status = StatusProcessamento.Revisar;
                resultado.Mensagem = "Competência não identificada no documento";
                _log.Aviso(resultado.Mensagem);
            }
            else if (!dados.TemSigla)
            {
                resultado.Status = StatusProcessamento.Revisar;
                resultado.Mensagem = "Tipo de documento não identificado";
                _log.Aviso(resultado.Mensagem);
            }
            else
            {
                var estrutura = _mapeamento.ObterMapeamento();
                var nomeNormalizado = _normalizacao.NormalizarNome(dados.Colaborador!);
                var colaboradoresCompativeis = _mapeamento.BuscarColaboradoresCompativeis(nomeNormalizado);

                if (colaboradoresCompativeis.Count == 0)
                {
                    var r = await CriarColaboradorENovoAsync(caminhoPdf, dados, estrutura);
                    resultado.Status = r.Status;
                    resultado.Mensagem = r.Mensagem;
                    resultado.CaminhoDestino = r.CaminhoDestino;
                }
                else if (colaboradoresCompativeis.Count > 1)
                {
                    resultado.Status = StatusProcessamento.Revisar;
                    resultado.Mensagem = $"Múltiplas pastas encontradas para '{dados.Colaborador}': " +
                        string.Join(", ", colaboradoresCompativeis.Select(c => c.NomePasta));
                    _log.Aviso(resultado.Mensagem);
                }
                else
                {
                    var colaborador = colaboradoresCompativeis[0];
                    var competencia = dados.Competencia!;

                    var pastaAno = _fileService.BuscarPastaAno(competencia.Ano!.Value, colaborador.CaminhoCompleto);
                    var pastaMes = _fileService.BuscarPastaMes(competencia.Mes!.Value, competencia.Ano!.Value, pastaAno);

                    var nomeArquivo = GerarNomeArquivo(dados, competencia);
                    var caminhoDestino = Path.Combine(pastaMes, nomeArquivo);

                    if (_fileService.ArquivoExiste(caminhoDestino))
                    {
                        resultado.Status = StatusProcessamento.Revisar;
                        resultado.Mensagem = $"Arquivo de destino já existe: {caminhoDestino}";
                        _log.Aviso(resultado.Mensagem);
                    }
                    else
                    {
                        _fileService.MoverArquivo(caminhoPdf, caminhoDestino);
                        resultado.Status = StatusProcessamento.Sucesso;
                        resultado.CaminhoDestino = caminhoDestino;
                        resultado.Mensagem = $"Documento movido com sucesso para: {caminhoDestino}";
                        _log.Informacao(resultado.Mensagem);
                    }
                }
            }

            if (resultado.Status == StatusProcessamento.Revisar && File.Exists(caminhoPdf))
            {
                var config = _configuracao.ObterConfiguracao();
                var pastaRevisar = Path.Combine(config.PastaRaiz, config.PastaRevisar);
                _fileService.CriarPasta(pastaRevisar);
                var nomeArquivo = Path.GetFileName(caminhoPdf);
                var destinoRevisao = Path.Combine(pastaRevisar, nomeArquivo);
                _fileService.MoverArquivo(caminhoPdf, destinoRevisao, sobrescrever: true);
                resultado.Mensagem += $" | Movido para revisão: {destinoRevisao}";
                _log.Informacao($"Documento movido para revisão: {destinoRevisao}");
            }
        }
        catch (Exception ex)
        {
            resultado.Status = StatusProcessamento.Erro;
            resultado.Mensagem = $"Erro ao processar documento individual: {ex.Message}";
            _log.Erro(resultado.Mensagem, ex);
        }

        return resultado;
    }

    private void LimparTemp(string pastaTemp)
    {
        try
        {
            if (Directory.Exists(pastaTemp))
                Directory.Delete(pastaTemp, true);
            _log.Debug($"Pasta temp limpa: {pastaTemp}");
        }
        catch (Exception ex)
        {
            _log.Aviso($"Não foi possível limpar pasta temp: {pastaTemp} - {ex.Message}");
        }
    }

    public async Task<List<ResultadoProcessamento>> ProcessarLoteAsync(
        List<string> arquivos, IProgress<ProgressoProcessamento>? progress = null)
    {
        var resultados = new List<ResultadoProcessamento>();
        var total = arquivos.Count;
        var processados = 0;

        foreach (var arquivo in arquivos)
        {
            var resultadosPdf = await ProcessarPdfCompletoAsync(arquivo);
            resultados.AddRange(resultadosPdf);

            processados++;
            progress?.Report(new ProgressoProcessamento
            {
                Total = total,
                Processados = processados,
                ArquivoAtual = Path.GetFileName(arquivo)
            });
        }

        return resultados;
    }

    private async Task<ResultadoProcessamento> CriarColaboradorENovoAsync(
        string caminhoPdf, DocumentoFinanceiro dados, EstruturaPasta estrutura)
    {
        var resultado = new ResultadoProcessamento
        {
            ArquivoOrigem = caminhoPdf,
            DadosExtraidos = dados
        };

        try
        {
            var config = _configuracao.ObterConfiguracao();
            var pastaColaboradores = Path.Combine(config.PastaRaiz, config.PastaColaboradores);
            var nomePasta = dados.Colaborador!.ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
            var novoCaminho = Path.Combine(pastaColaboradores, nomePasta);

            _fileService.CriarPasta(novoCaminho);

            _mapeamento.AtualizarMapeamento();

            var competencia = dados.Competencia!;
            var pastaAno = _fileService.BuscarPastaAno(competencia.Ano!.Value, novoCaminho);
            var pastaMes = _fileService.BuscarPastaMes(competencia.Mes!.Value, competencia.Ano!.Value, pastaAno);

            var nomeArquivo = GerarNomeArquivo(dados, competencia);
            var caminhoDestino = Path.Combine(pastaMes, nomeArquivo);

            _fileService.MoverArquivo(caminhoPdf, caminhoDestino);

            resultado.Status = StatusProcessamento.Sucesso;
            resultado.CaminhoDestino = caminhoDestino;
            resultado.Mensagem = $"Novo colaborador criado e documento movido: {caminhoDestino}";
            _log.Informacao(resultado.Mensagem);
        }
        catch (Exception ex)
        {
            resultado.Status = StatusProcessamento.Erro;
            resultado.Mensagem = $"Erro ao criar colaborador: {ex.Message}";
            _log.Erro(resultado.Mensagem, ex);
        }

        return resultado;
    }

    private string GerarNomeArquivo(DocumentoFinanceiro dados, Competencia competencia)
    {
        var sigla = dados.Sigla ?? "XX";
        var nomeColaborador = dados.Colaborador!.Replace(" ", "_");
        var nomeBase = $"{sigla}_{nomeColaborador}_{competencia.Mes:D2}-{competencia.Ano}";

        if (dados.Sigla == "OS" && !string.IsNullOrWhiteSpace(dados.NumeroOS))
        {
            nomeBase = $"OS_{nomeColaborador}_OS-{dados.NumeroOS}";
        }

        return $"{nomeBase}.pdf";
    }
}
