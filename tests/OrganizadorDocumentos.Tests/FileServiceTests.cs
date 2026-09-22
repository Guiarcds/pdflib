namespace OrganizadorDocumentos.Tests.Services;

using iText.Kernel.Pdf;
using ImageMagick;
using OrganizadorDocumentos.Core.Enums;
using OrganizadorDocumentos.Core.Models;
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

    [Fact]
    public async Task AplicarCorrecoesAsync_DescartaEPaginas_RotacionaCorretamente()
    {
        var caminhoPdf = CriarPdfTeste(3);
        var pastaTemp = Path.Combine(_pastaTeste, "correcoes", Guid.NewGuid().ToString("N"));
        var decisoes = new List<PageDecision>
        {
            new() { Page = 1, Action = PageAction.Keep, Rotation = 0 },
            new() { Page = 2, Action = PageAction.Discard, Reason = "blank" },
            new() { Page = 3, Action = PageAction.Rotate, Rotation = 180 }
        };

        var imagens = await _service.AplicarCorrecoesAsync(caminhoPdf, decisoes, pastaTemp);

        Assert.Equal(2, imagens.Count);
        Assert.DoesNotContain(imagens, p => Path.GetFileName(p).Contains("p2"));
        Assert.Contains(imagens, p => Path.GetFileName(p).Contains("p1"));
        Assert.Contains(imagens, p => Path.GetFileName(p).Contains("p3"));
        foreach (var img in imagens)
        {
            Assert.True(File.Exists(img));
        }
    }

    [Fact]
    public async Task AplicarCorrecoesAsync_TodasKeep_RetencaoCompleta()
    {
        var caminhoPdf = CriarPdfTeste(2);
        var pastaTemp = Path.Combine(_pastaTeste, "correcoes", Guid.NewGuid().ToString("N"));
        var decisoes = new List<PageDecision>
        {
            new() { Page = 1, Action = PageAction.Keep, Rotation = 0 },
            new() { Page = 2, Action = PageAction.Keep, Rotation = 0 }
        };

        var imagens = await _service.AplicarCorrecoesAsync(caminhoPdf, decisoes, pastaTemp);

        Assert.Equal(2, imagens.Count);
        Assert.All(imagens, img => Assert.True(File.Exists(img)));
    }

    [Fact]
    public async Task AplicarCorrecoesAsync_TodasDescartadas_RetornaColecaoVazia()
    {
        var caminhoPdf = CriarPdfTeste(3);
        var pastaTemp = Path.Combine(_pastaTeste, "correcoes", Guid.NewGuid().ToString("N"));
        var decisoes = new List<PageDecision>
        {
            new() { Page = 1, Action = PageAction.Discard, Reason = "blank" },
            new() { Page = 2, Action = PageAction.Discard, Reason = "blank" },
            new() { Page = 3, Action = PageAction.Discard, Reason = "blank" }
        };

        var imagens = await _service.AplicarCorrecoesAsync(caminhoPdf, decisoes, pastaTemp);

        Assert.Empty(imagens);
    }

    [Fact]
    public async Task SplitUmaPaginaPorPdfAsync_ConverteImagensParaPdf()
    {
        var pastaTemp = Path.Combine(_pastaTeste, "split", Guid.NewGuid().ToString("N"));
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var imagens = new List<string>
        {
            CriarImagemTeste($"corrected_p1_{timestamp}.jpg"),
            CriarImagemTeste($"corrected_p3_{timestamp}.jpg")
        };

        var pdfs = await _service.SplitUmaPaginaPorPdfAsync(imagens, pastaTemp);

        Assert.Equal(2, pdfs.Count);
        Assert.All(pdfs, pdf => Assert.True(File.Exists(pdf)));
        Assert.All(pdfs, pdf => Assert.EndsWith(".pdf", pdf));
        Assert.Contains(pdfs, p => Path.GetFileName(p).Contains("1"));
        Assert.Contains(pdfs, p => Path.GetFileName(p).Contains("3"));
    }

    [Fact]
    public async Task SplitUmaPaginaPorPdfAsync_ListaVazia_RetornaColecaoVazia()
    {
        var pastaTemp = Path.Combine(_pastaTeste, "split", Guid.NewGuid().ToString("N"));

        var pdfs = await _service.SplitUmaPaginaPorPdfAsync(new List<string>(), pastaTemp);

        Assert.Empty(pdfs);
    }

    [Fact]
    public async Task SplitUmaPaginaPorPdfAsync_CriaPastaSplit()
    {
        var pastaTemp = Path.Combine(_pastaTeste, "split_new", Guid.NewGuid().ToString("N"));
        var imagem = CriarImagemTeste("corrected_p1_test.jpg");

        var pdfs = await _service.SplitUmaPaginaPorPdfAsync(new List<string> { imagem }, pastaTemp);

        Assert.Single(pdfs);
        Assert.True(Directory.Exists(Path.Combine(pastaTemp, "split")));
    }

    private string CriarPdfTeste(int numPages)
    {
        var path = Path.Combine(_pastaTeste, $"test_pdf_{Guid.NewGuid():N}.pdf");
        using var writer = new PdfWriter(path);
        using var pdf = new PdfDocument(writer);
        for (int i = 0; i < numPages; i++)
        {
            pdf.AddNewPage();
        }
        pdf.Close();
        Assert.True(File.Exists(path));
        return path;
    }

    private string CriarImagemTeste(string nomeArquivo)
    {
        var path = Path.Combine(_pastaTeste, nomeArquivo);
        using var image = new MagickImage(MagickColors.White, 100, 100);
        image.Write(path, MagickFormat.Jpeg);
        return path;
    }
}
