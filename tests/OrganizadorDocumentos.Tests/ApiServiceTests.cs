namespace OrganizadorDocumentos.Tests.Services;

using System.Net;
using System.Net.Http;
using System.Text;
using iText.Kernel.Pdf;
using Newtonsoft.Json;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services;
using OrganizadorDocumentos.Core.Services.Interfaces;
using OrganizadorDocumentos.Core.Enums;
using Xunit;

public class ApiServiceTests
{
    private readonly string _pastaTeste = Path.Combine(Path.GetTempPath(), "OrganizadorDocs_ApiTests_" + Guid.NewGuid().ToString("N"));

    public ApiServiceTests()
    {
        Directory.CreateDirectory(_pastaTeste);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pastaTeste))
        {
            Directory.Delete(_pastaTeste, true);
        }
    }

    [Fact]
    public async Task AnalisarEstruturaPdf_ReturnsDecisions()
    {
        var caminhoPdf = CriarPdfTeste(3);
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "test-key",
            ApiModel = "test-model",
            PastaRaiz = _pastaTeste
        });

        var fakeContent = """
        [
          {"page": 1, "action": "keep", "rotation": 0},
          {"page": 2, "action": "discard", "reason": "blank"},
          {"page": 3, "action": "rotate", "rotation": 180}
        ]
        """;

        var fakeResponse = CriarRespostaFake(fakeContent);
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
        var logPath = Path.Combine(_pastaTeste, "test.log");
        var log = new LogService(logPath);
        var apiService = new ApiService(configuracao, log, httpClient);

        var decisoes = await apiService.AnalisarEstruturaPdfAsync(caminhoPdf);

        Assert.Equal(3, decisoes.Count);
        Assert.Equal(PageAction.Keep, decisoes[0].Action);
        Assert.Equal(0, decisoes[0].Rotation);
        Assert.Equal(PageAction.Discard, decisoes[1].Action);
        Assert.Equal("blank", decisoes[1].Reason);
        Assert.Equal(PageAction.Rotate, decisoes[2].Action);
        Assert.Equal(180, decisoes[2].Rotation);
    }

    [Fact]
    public async Task AnalisarEstruturaPdf_RespostaComObjetoUnico_RetornaDecisaoUnica()
    {
        var caminhoPdf = CriarPdfTeste(1);
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "test-key",
            ApiModel = "test-model",
            PastaRaiz = _pastaTeste
        });

        var fakeContent = """
        {"page": 1, "action": "keep", "rotation": 0}
        """;

        var fakeResponse = CriarRespostaFake(fakeContent);
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
        var logPath = Path.Combine(_pastaTeste, "test.log");
        var log = new LogService(logPath);
        var apiService = new ApiService(configuracao, log, httpClient);

        var decisoes = await apiService.AnalisarEstruturaPdfAsync(caminhoPdf);

        Assert.Single(decisoes);
        Assert.Equal(PageAction.Keep, decisoes[0].Action);
        Assert.Equal(1, decisoes[0].Page);
    }

    [Fact]
    public async Task AnalisarEstruturaPdf_SemApiKey_LancaExcecao()
    {
        var caminhoPdf = CriarPdfTeste(1);
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "",
            ApiModel = "test-model",
            PastaRaiz = _pastaTeste
        });

        var logPath = Path.Combine(_pastaTeste, "test.log");
        var log = new LogService(logPath);
        var apiService = new ApiService(configuracao, log);

        await Assert.ThrowsAsync<InvalidOperationException>(() => apiService.AnalisarEstruturaPdfAsync(caminhoPdf));
    }

    [Fact]
    public async Task ExtrairDadosDocumentoAsync_FakeResponse_RetornaDocumento()
    {
        var caminhoPdf = CriarPdfTeste(1);
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "test-key",
            ApiModel = "test-model",
            PastaRaiz = _pastaTeste
        });

        var fakeContent = """
        [
          {
            "colaborador": "João da Silva",
            "tipo_documento": "Recibo de Pagamento",
            "sigla": "VT",
            "competencia": { "mes": 9, "ano": 2026 },
            "data": "01/09/2026",
            "numero_os": null,
            "confianca": 0.95
          }
        ]
        """;

        var fakeResponse = CriarRespostaFake(fakeContent);
        using var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
        var logPath = Path.Combine(_pastaTeste, "test.log");
        var log = new LogService(logPath);
        var apiService = new ApiService(configuracao, log, httpClient);

        var documento = await apiService.ExtrairDadosDocumentoAsync(caminhoPdf);

        Assert.NotNull(documento);
        Assert.Equal("João da Silva", documento.Colaborador);
        Assert.Equal("VT", documento.Sigla);
        Assert.Equal(9, documento.Competencia?.Mes);
        Assert.Equal(2026, documento.Competencia?.Ano);
    }

    [Fact]
    public async Task ExtrairDadosDocumentoAsync_RegistraTokensDaResposta()
    {
        var caminhoPdf = CriarPdfTeste(1);
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "test-key",
            ApiModel = "test-model",
            PastaRaiz = _pastaTeste
        });

        var fakeResponse = CriarRespostaFake("[]", new
        {
            prompt_tokens = 1200,
            completion_tokens = 340,
            total_tokens = 1540,
            prompt_tokens_details = new { cached_tokens = 15 },
            completion_tokens_details = new { reasoning_tokens = 12 },
            cost = 0.00123
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
        var logPath = Path.Combine(_pastaTeste, "test.log");
        var log = new LogService(logPath);
        var apiService = new ApiService(configuracao, log, httpClient);

        await apiService.ExtrairDadosDocumentoAsync(caminhoPdf);

        Assert.Equal(1200, apiService.UsoAcumulado.PromptTokens);
        Assert.Equal(340, apiService.UsoAcumulado.CompletionTokens);
        Assert.Equal(1540, apiService.UsoAcumulado.TotalTokens);
        Assert.Equal(12, apiService.UsoAcumulado.ReasoningTokens);
        Assert.Equal(15, apiService.UsoAcumulado.CachedTokens);
        Assert.Equal(0.00123, apiService.UsoAcumulado.Custo!.Value, 5);
        Assert.Equal(1540, apiService.UltimoUso.TotalTokens);
    }

    [Fact]
    public async Task ResetarUso_ZeraContadoresDeTokens()
    {
        var caminhoPdf = CriarPdfTeste(1);
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "test-key",
            ApiModel = "test-model",
            PastaRaiz = _pastaTeste
        });

        var fakeResponse = CriarRespostaFake("[]", new
        {
            prompt_tokens = 100,
            completion_tokens = 50,
            total_tokens = 150,
            cost = 0.001
        });

        using var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
        var logPath = Path.Combine(_pastaTeste, "test.log");
        var log = new LogService(logPath);
        var apiService = new ApiService(configuracao, log, httpClient);

        await apiService.ExtrairDadosDocumentoAsync(caminhoPdf);
        Assert.Equal(150, apiService.UsoAcumulado.TotalTokens);

        apiService.ResetarUso();

        Assert.Equal(0, apiService.UsoAcumulado.TotalTokens);
        Assert.Equal(0, apiService.UltimoUso.TotalTokens);
        Assert.Null(apiService.UsoAcumulado.Custo);
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
        return path;
    }

    private HttpResponseMessage CriarRespostaFake(string content, object? usage = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["choices"] = new[]
            {
                new
                {
                    message = new
                    {
                        content = content
                    }
                }
            }
        };

        if (usage != null)
            payload["usage"] = usage;

        var jsonResponse = JsonConvert.SerializeObject(payload);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };
    }
}

public class TestConfiguracaoService : IConfiguracaoService
{
    private readonly AppConfig _config;

    public TestConfiguracaoService(AppConfig config)
    {
        _config = config;
    }

    public AppConfig CarregarConfiguracao() => _config;
    public void SalvarConfiguracao(AppConfig config) { }
    public AppConfig ObterConfiguracao() => _config;
    public void AtualizarConfiguracao(Action<AppConfig> atualizador) { }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
