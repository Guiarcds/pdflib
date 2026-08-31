namespace OrganizadorDocumentos.Core.Services;

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class ApiService : IApiService
{
    private readonly IConfiguracaoService _configuracao;
    private readonly ILogService _log;
    private readonly HttpClient _httpClient;

    private const string SystemPrompt = @"Você é um especialista em documentos financeiros brasileiros.
Analise o PDF fornecido e extraia APENAS as informações solicitadas.
NÃO invente informações. Se não encontrar, retorne null.

Retorne APENAS um JSON válido com a seguinte estrutura:
{
  ""colaborador"": ""nome completo ou null"",
  ""tipo_documento"": ""descrição do tipo ou null"",
  ""sigla"": ""VT|VA|AC|BO|CO|SP|DE|SE|SB|OS|null"",
  ""competencia"": {
    ""mes"": 1-12 ou null,
    ""ano"": YYYY ou null
  },
  ""data"": ""DD/MM/YYYY ou null"",
  ""numero_os"": ""número ou null"",
  ""confianca"": 0.0-1.0
}

Siglas: VT=Vale Transporte, VA=Vale Alimentação, AC=Ajuda de Custo,
BO=Bonificação, CO=Comissão, SP=Serviço Prestado, DE=Diária,
SE=Salário Extra, SB=Salário Base, OS=Vale por OS

Retorne APENAS o JSON, sem explicações adicionais.";

    public ApiService(IConfiguracaoService configuracao, ILogService log, HttpClient? httpClient = null)
    {
        _configuracao = configuracao;
        _log = log;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    public async Task<DocumentoFinanceiro> ExtrairDadosAsync(string caminhoPdf)
    {
        var config = _configuracao.ObterConfiguracao();

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("API Key não configurada");
        }

        _log.Informacao($"Extraindo dados do PDF: {Path.GetFileName(caminhoPdf)}");

        try
        {
            var textoPdf = await LerTextoPdfAsync(caminhoPdf);

            if (string.IsNullOrWhiteSpace(textoPdf))
            {
                _log.Aviso($"Não foi possível extrair texto do PDF: {caminhoPdf}");
                return new DocumentoFinanceiro { Confianca = 0 };
            }

            return await EnviarParaIAAsync(textoPdf, config);
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao processar PDF: {caminhoPdf}", ex);
            throw;
        }
    }

    private async Task<string> LerTextoPdfAsync(string caminhoPdf)
    {
        try
        {
            using var reader = new iText.Kernel.Pdf.PdfReader(caminhoPdf);
            using var document = new iText.Kernel.Pdf.PdfDocument(reader);

            var texto = new StringBuilder();

            for (int i = 1; i <= document.GetNumberOfPages(); i++)
            {
                var pagina = document.GetPage(i);
                var textoPagina = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pagina);
                texto.AppendLine(textoPagina);
            }

            return texto.ToString();
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao ler PDF localmente: {caminhoPdf}", ex);
            return string.Empty;
        }
    }

    private async Task<DocumentoFinanceiro> EnviarParaIAAsync(string textoPdf, AppConfig config)
    {
        var request = new
        {
            model = config.ApiModel,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Analise este documento financeiro e extraia as informações:\n\n{textoPdf}" }
            },
            temperature = 0.1,
            max_tokens = 1000
        };

        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://organizador-documentos.local");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Organizador de Documentos");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _log.Erro($"Erro na API OpenRouter: {response.StatusCode} - {jsonResponse}");
            throw new HttpRequestException($"Erro na API: {response.StatusCode}");
        }

        return ParseRespostaIA(jsonResponse);
    }

    private DocumentoFinanceiro ParseRespostaIA(string jsonResponse)
    {
        try
        {
            var jsonObject = JObject.Parse(jsonResponse);
            var content = jsonObject["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(content))
            {
                _log.Aviso("Resposta da IA vazia");
                return new DocumentoFinanceiro { Confianca = 0 };
            }

            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (!jsonMatch.Success)
            {
                _log.Aviso("JSON não encontrado na resposta da IA");
                return new DocumentoFinanceiro { Confianca = 0 };
            }

            var dados = JObject.Parse(jsonMatch.Value);

            return new DocumentoFinanceiro
            {
                Colaborador = dados["colaborador"]?.ToString(),
                TipoDocumento = dados["tipo_documento"]?.ToString(),
                Sigla = dados["sigla"]?.ToString()?.ToUpperInvariant(),
                Competencia = new Competencia
                {
                    Mes = dados["competencia"]?["mes"]?.ToObject<int?>(),
                    Ano = dados["competencia"]?["ano"]?.ToObject<int?>()
                },
                Data = dados["data"]?.ToString(),
                NumeroOS = dados["numero_os"]?.ToString(),
                Confianca = dados["confianca"]?.ToObject<double>() ?? 0.0
            };
        }
        catch (Exception ex)
        {
            _log.Erro("Erro ao parsear resposta da IA", ex);
            return new DocumentoFinanceiro { Confianca = 0 };
        }
    }
}
