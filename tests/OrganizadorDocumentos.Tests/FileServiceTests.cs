namespace OrganizadorDocumentos.Tests.Services;

using OrganizadorDocumentos.Core.Services;
using OrganizadorDocumentos.Core.Services.Interfaces;
using Xunit;

public class FileServiceTests
{
    private readonly NormalizacaoService _normalizacao = new();
    private readonly string _pastaTeste = Path.Combine(Path.GetTempPath(), "OrganizadorDocs_Tests_" + Guid.NewGuid().ToString("N"));

    private ILogService _log;
    private FileService _service;

    public FileServiceTests()
    {
        var logPath = Path.Combine(_pastaTeste, "test.log");
        Directory.CreateDirectory(_pastaTeste);
        _log = new LogService(logPath);
        _service = new FileService(_normalizacao, _log);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pastaTeste))
        {
            Directory.Delete(_pastaTeste, true);
        }
    }

    [Fact]
    public void BuscarPastaAno_PastaExistente_UsaExistente()
    {
        var pastaColaborador = Path.Combine(_pastaTeste, "JOAO_SILVA");
        Directory.CreateDirectory(Path.Combine(pastaColaborador, "2026"));

        var resultado = _service.BuscarPastaAno(2026, pastaColaborador);

        Assert.Equal(Path.Combine(pastaColaborador, "2026"), resultado);
    }

    [Fact]
    public void BuscarPastaAno_PastaNaoExistente_CriaNova()
    {
        var pastaColaborador = Path.Combine(_pastaTeste, "MARIA_SOUZA");
        Directory.CreateDirectory(pastaColaborador);

        var resultado = _service.BuscarPastaAno(2025, pastaColaborador);

        Assert.Equal(Path.Combine(pastaColaborador, "2025"), resultado);
        Assert.True(Directory.Exists(resultado));
    }

    [Fact]
    public void BuscarPastaMes_PastaExistente_UsaExistente()
    {
        var pastaAno = Path.Combine(_pastaTeste, "2026");
        Directory.CreateDirectory(Path.Combine(pastaAno, "08 - Agosto"));

        var resultado = _service.BuscarPastaMes(8, 2026, pastaAno);

        Assert.Equal(Path.Combine(pastaAno, "08 - Agosto"), resultado);
    }

    [Fact]
    public void BuscarPastaMes_VariacaoNomenclatura_ReconheceEquivalente()
    {
        var pastaAno = Path.Combine(_pastaTeste, "2026");
        Directory.CreateDirectory(Path.Combine(pastaAno, "Agosto"));

        var resultado = _service.BuscarPastaMes(8, 2026, pastaAno);

        Assert.Equal(Path.Combine(pastaAno, "Agosto"), resultado);
    }

    [Fact]
    public void BuscarPastaMes_PastaNaoExistente_CriaNova()
    {
        var pastaAno = Path.Combine(_pastaTeste, "2026");
        Directory.CreateDirectory(pastaAno);

        var resultado = _service.BuscarPastaMes(3, 2026, pastaAno);

        Assert.True(Directory.Exists(resultado));
        Assert.Contains("Março", resultado);
    }

    [Fact]
    public void ListarPdfs_RetornaApenasPdfs()
    {
        var pasta = Path.Combine(_pastaTeste, "ENTRADA");
        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, "doc1.pdf"), "teste");
        File.WriteAllText(Path.Combine(pasta, "doc2.pdf"), "teste");
        File.WriteAllText(Path.Combine(pasta, "outro.txt"), "teste");

        var resultado = _service.ListarPdfs(pasta);

        Assert.Equal(2, resultado.Count);
    }
}
