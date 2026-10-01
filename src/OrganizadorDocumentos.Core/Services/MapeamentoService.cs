namespace OrganizadorDocumentos.Core.Services;

using System.Text.RegularExpressions;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class MapeamentoService : IMapeamentoService
{
    private readonly INormalizacaoService _normalizacao;
    private readonly IFileService _fileService;
    private readonly ILogService _log;
    private EstruturaPasta _estrutura = new();

    public event EventHandler<MapeamentoEventArgs>? MapeamentoAtualizado;

    public MapeamentoService(INormalizacaoService normalizacao, IFileService fileService, ILogService log)
    {
        _normalizacao = normalizacao;
        _fileService = fileService;
        _log = log;
    }

    public EstruturaPasta MapearEstrutura(string pastaRaiz)
    {
        _log.Informacao($"Iniciando mapeamento da estrutura: {pastaRaiz}");

        var estrutura = new EstruturaPasta();

        if (!Directory.Exists(pastaRaiz))
        {
            _log.Aviso($"Pasta raiz não encontrada: {pastaRaiz}");
            return estrutura;
        }

        var pastaColaboradores = Path.Combine(pastaRaiz, "COLABORADORES");
        if (!Directory.Exists(pastaColaboradores))
        {
            _log.Aviso($"Pasta COLABORADORES não encontrada em: {pastaRaiz}");
            return estrutura;
        }

        var diretoriosColaboradores = Directory.GetDirectories(pastaColaboradores);

        foreach (var dirColaborador in diretoriosColaboradores)
        {
            var nomePasta = Path.GetFileName(dirColaborador);
            var colaborador = new Colaborador
            {
                NomePasta = nomePasta,
                NomeNormalizado = _normalizacao.NormalizarNome(nomePasta),
                CaminhoCompleto = dirColaborador
            };

            var diretoriosAnos = Directory.GetDirectories(dirColaborador);
            foreach (var dirAno in diretoriosAnos)
            {
                var nomeAno = Path.GetFileName(dirAno);
                if (Regex.IsMatch(nomeAno, @"^\d{4}"))
                {
                    var ano = int.Parse(nomeAno.Substring(0, 4));
                    var pastaAno = new PastaAno
                    {
                        Ano = ano,
                        NomePasta = nomeAno,
                        CaminhoCompleto = dirAno
                    };

                    var diretoriosMeses = Directory.GetDirectories(dirAno);
                    foreach (var dirMes in diretoriosMeses)
                    {
                        var nomeMes = Path.GetFileName(dirMes);
                        var mes = ExtrairNumeroMes(nomeMes);
                        if (mes > 0)
                        {
                            pastaAno.Meses.Add(new PastaMes
                            {
                                NumeroMes = mes,
                                NomePasta = nomeMes,
                                NomeNormalizado = _normalizacao.NormalizarNome(nomeMes),
                                CaminhoCompleto = dirMes
                            });
                        }
                    }

                    colaborador.Anos.Add(pastaAno);
                }
            }

            estrutura.Colaboradores.Add(colaborador);
        }

        _estrutura = estrutura;

        _log.Informacao($"Mapeamento concluído: {estrutura.TotalColaboradores} colaboradores, " +
                        $"{estrutura.TotalAnos} anos, {estrutura.TotalPastasMensais} pastas mensais");

        MapeamentoAtualizado?.Invoke(this, new MapeamentoEventArgs { Estrutura = estrutura });

        return estrutura;
    }

    public void AtualizarMapeamento()
    {
        var caminho = _estrutura.Colaboradores
            .Select(c => c.CaminhoCompleto)
            .FirstOrDefault(c => !string.IsNullOrEmpty(c) && Directory.Exists(c));

        if (caminho == null)
            return;

        var pastaRaiz = Path.GetDirectoryName(Path.GetDirectoryName(caminho));
        if (pastaRaiz != null)
            MapearEstrutura(pastaRaiz);
    }

    public EstruturaPasta ObterMapeamento()
    {
        return _estrutura;
    }

    public List<Colaborador> BuscarColaboradoresCompativeis(string nomeNormalizado)
    {
        var resultados = new List<Colaborador>();

        foreach (var colaborador in _estrutura.Colaboradores)
        {
            if (_normalizacao.SãoEquivalentes(colaborador.NomeNormalizado, nomeNormalizado))
            {
                resultados.Add(colaborador);
            }
        }

        return resultados;
    }

    public UnificarResultado UnificarColaboradores(string nomeOrigem, string nomeDestino)
    {
        var resultado = new UnificarResultado
        {
            NomeOrigem = nomeOrigem ?? string.Empty,
            NomeDestino = nomeDestino ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(nomeOrigem) || string.IsNullOrWhiteSpace(nomeDestino))
        {
            resultado.Mensagem = "Informe o colaborador de origem e o de destino.";
            return resultado;
        }

        if (string.Equals(nomeOrigem.Trim(), nomeDestino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            resultado.Mensagem = "O colaborador de origem e o de destino não podem ser o mesmo.";
            return resultado;
        }

        var origem = BuscarColaboradorPorNome(nomeOrigem);
        var destino = BuscarColaboradorPorNome(nomeDestino);

        if (origem == null)
        {
            resultado.Mensagem = $"Colaborador de origem não encontrado no mapeamento: {nomeOrigem}";
            return resultado;
        }

        if (destino == null)
        {
            resultado.Mensagem = $"Colaborador de destino não encontrado no mapeamento: {nomeDestino}";
            return resultado;
        }

        if (!Directory.Exists(origem.CaminhoCompleto))
        {
            resultado.Mensagem = $"Pasta do colaborador de origem não encontrada: {origem.CaminhoCompleto}";
            return resultado;
        }

        if (string.Equals(origem.CaminhoCompleto, destino.CaminhoCompleto, StringComparison.OrdinalIgnoreCase))
        {
            resultado.Mensagem = "Os dois colaboradores apontam para a mesma pasta.";
            return resultado;
        }

        try
        {
            _log.Informacao($"Unificando colaborador '{origem.NomePasta}' em '{destino.NomePasta}'");

            var movido = _fileService.MoverPastaCompleta(origem.CaminhoCompleto, destino.CaminhoCompleto);

            resultado.ArquivosMovidos = movido.ArquivosMovidos;
            resultado.ConflitosRenomeados = movido.ConflitosRenomeados;
            resultado.PastasMesCriadas = movido.PastasMesCriadas;
            resultado.PastasMesReutilizadas = movido.PastasMesReutilizadas;
            resultado.Erros.AddRange(movido.Erros);

            if (!movido.Sucesso)
            {
                resultado.Mensagem = $"Unificação concluída parcialmente: {movido.Erros.Count} erro(s). " +
                                     $"A pasta '{origem.NomePasta}' foi preservada.";
                _log.Erro(resultado.Mensagem);
                return resultado;
            }

            AtualizarMapeamento();

            resultado.Sucesso = true;
            resultado.Mensagem = $"'{origem.NomePasta}' unificado em '{destino.NomePasta}': " +
                                 $"{movido.ArquivosMovidos} arquivo(s) movido(s), " +
                                 $"{movido.ConflitosRenomeados} renomeado(s) por conflito, " +
                                 $"{movido.PastasMesCriadas} pasta(s) de mês criada(s).";

            _log.Informacao(resultado.Mensagem);
        }
        catch (Exception ex)
        {
            resultado.Mensagem = $"Erro ao unificar colaboradores: {ex.Message}";
            _log.Erro($"Erro ao unificar '{nomeOrigem}' em '{nomeDestino}'", ex);
        }

        return resultado;
    }

    private Colaborador? BuscarColaboradorPorNome(string nomePasta)
    {
        return _estrutura.Colaboradores.FirstOrDefault(c =>
            string.Equals(c.NomePasta, nomePasta.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private int ExtrairNumeroMes(string nomePasta)
    {
        var match = Regex.Match(nomePasta, @"^(\d{1,2})");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var mes) && mes >= 1 && mes <= 12)
            return mes;

        var nomeLower = _normalizacao.NormalizarNome(nomePasta);
        var meses = new[]
        {
            "janeiro", "fevereiro", "marco", "abril", "maio", "junho",
            "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
        };

        for (int i = 0; i < meses.Length; i++)
        {
            if (nomeLower.Contains(meses[i]))
                return i + 1;
        }

        return 0;
    }
}
