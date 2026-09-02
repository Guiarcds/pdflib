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

    public async Task<ResultadoProcessamento> ProcessarDocumentoAsync(string caminhoPdf)
    {
        var resultado = new ResultadoProcessamento
        {
            ArquivoOrigem = caminhoPdf,
            Status = StatusProcessamento.Erro
        };

        try
        {
            _log.Informacao($"Processando: {Path.GetFileName(caminhoPdf)}");

            var dados = await _apiService.ExtrairDadosAsync(caminhoPdf);
            resultado.DadosExtraidos = dados;

            if (!dados.TemColaborador)
            {
                resultado.Status = StatusProcessamento.Revisar;
                resultado.Mensagem = "Colaborador não identificado no documento";
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
        }
        catch (Exception ex)
        {
            resultado.Status = StatusProcessamento.Erro;
            resultado.Mensagem = $"Erro ao processar documento: {ex.Message}";
            _log.Erro(resultado.Mensagem, ex);
        }

        if (resultado.Status == StatusProcessamento.Revisar && File.Exists(caminhoPdf))
        {
            var config = _configuracao.ObterConfiguracao();
            var pastaRevisar = Path.Combine(config.PastaRaiz, config.PastaRevisar);
            _fileService.CriarPasta(pastaRevisar);
            var nomeArquivo = Path.GetFileName(caminhoPdf);
            var destinoRevisao = Path.Combine(pastaRevisar, nomeArquivo);
            _fileService.MoverArquivo(caminhoPdf, destinoRevisao);
            resultado.Mensagem += $" | Movido para revisão: {destinoRevisao}";
            _log.Informacao($"Documento movido para revisão: {destinoRevisao}");
        }

        DocumentoProcessado?.Invoke(this, resultado);
        return resultado;
    }

    public async Task<List<ResultadoProcessamento>> ProcessarLoteAsync(
        List<string> arquivos, IProgress<ProgressoProcessamento>? progress = null)
    {
        var resultados = new List<ResultadoProcessamento>();
        var total = arquivos.Count;
        var processados = 0;

        foreach (var arquivo in arquivos)
        {
            var resultado = await ProcessarDocumentoAsync(arquivo);
            resultados.Add(resultado);

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
