namespace OrganizadorDocumentos.Core.Services;

using System.Text.RegularExpressions;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class FileService : IFileService
{
    private readonly INormalizacaoService _normalizacao;
    private readonly ILogService _log;

    public FileService(INormalizacaoService normalizacao, ILogService log)
    {
        _normalizacao = normalizacao;
        _log = log;
    }

    public bool PastaExiste(string caminho)
    {
        return Directory.Exists(caminho);
    }

    public bool ArquivoExiste(string caminho)
    {
        return File.Exists(caminho);
    }

    public string BuscarPastaAno(int ano, string caminhoColaborador)
    {
        if (!Directory.Exists(caminhoColaborador))
        {
            _log.Aviso($"Pasta do colaborador não encontrada: {caminhoColaborador}");
            throw new DirectoryNotFoundException($"Pasta não encontrada: {caminhoColaborador}");
        }

        var diretorios = Directory.GetDirectories(caminhoColaborador);
        var anoStr = ano.ToString();

        foreach (var dir in diretorios)
        {
            var nome = Path.GetFileName(dir);
            if (nome == anoStr)
            {
                _log.Debug($"Pasta ano encontrada (exata): {dir}");
                return dir;
            }
        }

        foreach (var dir in diretorios)
        {
            var nome = Path.GetFileName(dir);
            if (nome.StartsWith(anoStr) || Regex.IsMatch(nome, $"^{ano}[_\\s-]"))
            {
                _log.Debug($"Pasta ano encontrada (variação): {dir}");
                return dir;
            }
        }

        var novoCaminho = Path.Combine(caminhoColaborador, anoStr);
        Directory.CreateDirectory(novoCaminho);
        _log.Informacao($"Pasta ano criada: {novoCaminho}");
        return novoCaminho;
    }

    public string BuscarPastaMes(int mes, int ano, string caminhoAno)
    {
        if (!Directory.Exists(caminhoAno))
        {
            _log.Aviso($"Pasta do ano não encontrada: {caminhoAno}");
            throw new DirectoryNotFoundException($"Pasta não encontrada: {caminhoAno}");
        }

        var diretorios = Directory.GetDirectories(caminhoAno);
        var nomeMes = _normalizacao.NomeMesPorExtenso(mes);
        var nomesPossiveis = new[]
        {
            $"{mes:D2} - {nomeMes}",
            $"{mes:D2} {nomeMes}",
            $"{mes:D2}_{nomeMes}",
            nomeMes,
            mes.ToString("D2"),
            mes.ToString()
        };

        foreach (var dir in diretorios)
        {
            var nomePasta = Path.GetFileName(dir);
            var nomeNorm = _normalizacao.NormalizarNome(nomePasta);

            foreach (var possivel in nomesPossiveis)
            {
                if (nomeNorm == _normalizacao.NormalizarNome(possivel))
                {
                    _log.Debug($"Pasta mês encontrada: {dir}");
                    return dir;
                }
            }

            var match = Regex.Match(nomePasta, @"^(\d{1,2})");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var mesEncontrado) && mesEncontrado == mes)
            {
                _log.Debug($"Pasta mês encontrada (por número): {dir}");
                return dir;
            }
        }

        var novoNome = $"{mes:D2} - {nomeMes}";
        var novoCaminho = Path.Combine(caminhoAno, novoNome);
        Directory.CreateDirectory(novoCaminho);
        _log.Informacao($"Pasta mês criada: {novoCaminho}");
        return novoCaminho;
    }

    public void MoverArquivo(string origem, string destino)
    {
        if (!File.Exists(origem))
            throw new FileNotFoundException($"Arquivo não encontrado: {origem}");

        if (File.Exists(destino))
            throw new InvalidOperationException($"Arquivo de destino já existe: {destino}");

        var diretorioDestino = Path.GetDirectoryName(destino);
        if (!string.IsNullOrEmpty(diretorioDestino) && !Directory.Exists(diretorioDestino))
            Directory.CreateDirectory(diretorioDestino);

        File.Move(origem, destino);
        _log.Informacao($"Arquivo movido: {origem} -> {destino}");
    }

    public void CriarPasta(string caminho)
    {
        if (!Directory.Exists(caminho))
        {
            Directory.CreateDirectory(caminho);
            _log.Informacao($"Pasta criada: {caminho}");
        }
    }

    public List<string> ListarPdfs(string pasta)
    {
        if (!Directory.Exists(pasta))
        {
            _log.Aviso($"Pasta não encontrada: {pasta}");
            return new List<string>();
        }

        return Directory.GetFiles(pasta, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();
    }

    public string NomeArquivoUnico(string caminhoDestino, string nomeBase, string extensao)
    {
        var caminho = Path.Combine(caminhoDestino, $"{nomeBase}{extensao}");
        if (!File.Exists(caminho))
            return caminho;

        int contador = 1;
        do
        {
            caminho = Path.Combine(caminhoDestino, $"{nomeBase}_{contador}{extensao}");
            contador++;
        } while (File.Exists(caminho));

        return caminho;
    }
}
