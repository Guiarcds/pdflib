namespace OrganizadorDocumentos.Core.Services;

using System.Text.RegularExpressions;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class MapeamentoService : IMapeamentoService
{
    private readonly INormalizacaoService _normalizacao;
    private readonly ILogService _log;
    private EstruturaPasta _estrutura = new();

    public event EventHandler<MapeamentoEventArgs>? MapeamentoAtualizado;

    public MapeamentoService(INormalizacaoService normalizacao, ILogService log)
    {
        _normalizacao = normalizacao;
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
        if (!string.IsNullOrEmpty(_estrutura.Colaboradores.FirstOrDefault()?.CaminhoCompleto))
        {
            var pastaRaiz = Path.GetDirectoryName(Path.GetDirectoryName(
                _estrutura.Colaboradores.First().CaminhoCompleto));
            if (pastaRaiz != null)
                MapearEstrutura(pastaRaiz);
        }
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
