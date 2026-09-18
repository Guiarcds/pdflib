namespace OrganizadorDocumentos.Core.Services;

using System.Text;
using ImageMagick;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;
using Tesseract;

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

REGRAS PARA EXTRAÇÃO DE COMPETÊNCIA (MUITO IMPORTANTE):
- A COMPETÊNCIA é o período de referência do documento (mês/ano)
- Em documentos RECIBO, a competência aparece logo após o nome do benefício, precedida por ""Referente"" (ex: ""Recibo - Referente a 09/2025"")
- Procure por datas no formato MM/AAAA, MM/YYYY, DD/MM/AAAA em qualquer parte do documento
- Procure por nomes de meses em português (Janeiro, Fevereiro, Março, Abril, Maio, Junho, Julho, Agosto, Setembro, Outubro, Novembro, Dezembro) seguidos de um ano
- Se encontrar apenas o ano (AAAA) e um número de mês (1-12) em qualquer contexto, use-os como competência
- Se o texto estiver distorcido/ilegível, interprete o melhor possível — não retorne null apenas porque o texto parece ruim
- NÚMEROS QUE PARECEM DATA: ""08/08/2026"", ""20/08/2023"", ""08-08-2026"" são competências válidas
- IGNORE datas de pagamento se houver diferença clara com a data do benefício

REGRAS PARA EXTRAÇÃO DE COLABORADOR (MUITO IMPORTANTE):
- O COLABORADOR é quem **RECEBE** o valor/benefício (beneficiário/titular/favorecido)
- Procure por: ""Beneficiário"", ""Titular"", ""Funcionário"", ""Colaborador"", ""Empregado"", ""Trabalhador"", ""Favorecido"", ""Destinatário""
- **ATENÇÃO COM 'EMITENTE'**: Em RECIBOS, o campo ""Emitente"" pode indicar QUEM RECEBE (ex: ""Emitente: João da Silva""). Nestes casos, USE o nome após ""Emitente"". Mas em VALES/BOLETOS, ""Emitente"" é quem EMITE/PAGA (empresa/prefeitura) — NESTES CASOS IGNORE.
- Como distinguir: se o documento tem título ""RECIBO"" ou ""RECIBO DE PAGAMENTO"", ""Emitente"" = colaborador. Se é ""VALE"", ""BOLETO"", ""COMPROVANTE DE PAGAMENTO"", ""Emitente"" = empresa/pagador (ignore).
- **IGNORE SEMPRE**: nomes de empresas, bancos, órgãos públicos, prefeituras, secretarias, CNPJs como colaborador
- Nomes brasileiros podem ter: acentos (João, São, José), partículas (da, de, do, das, dos), sobrenomes compostos (Silva Santos, Costa Lima)
- Exemplos válidos: ""João da Silva"", ""Maria José dos Santos"", ""José Maria da Costa Lima""
- Se houver múltiplos nomes, escolha o que aparece como **titular/beneficiário/favorecido/emitente (em recibos)** do documento

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

            if (!string.IsNullOrWhiteSpace(textoPdf) && TextoContemCompetencia(textoPdf))
            {
                _log.Debug($"Texto iText7 suficiente ({textoPdf.Length} chars), enviando texto para IA");
                return await EnviarParaIAAsync(textoPdf, config);
            }

            _log.Informacao("Documento digitalizado/escrito à mão detectado, enviando imagens para IA...");
            return await EnviarParaIAComImagensAsync(caminhoPdf, config);
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao processar PDF: {caminhoPdf}", ex);
            throw;
        }
    }

    private static readonly string[] MesesPortugues = {
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
        "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"
    };

    private async Task<string> LerTextoPdfAsync(string caminhoPdf)
    {
        try
        {
            var texto = await LerComItextAsync(caminhoPdf);

            if (!string.IsNullOrWhiteSpace(texto) && TextoContemCompetencia(texto))
                return texto;

            if (!string.IsNullOrWhiteSpace(texto))
                _log.Aviso("Texto iText7 sem competência detectada, tentando OCR...");
            else
                _log.Aviso("Texto vazio via iText7, tentando OCR...");

            var textoOcr = await LerComOcrAsync(caminhoPdf);

            if (!string.IsNullOrWhiteSpace(textoOcr))
            {
                _log.Informacao($"OCR concluído: {textoOcr.Length} caracteres extraídos");
                return textoOcr;
            }

            if (!string.IsNullOrWhiteSpace(texto))
                _log.Aviso("OCR não melhorou o texto, usando texto iText7 mesmo assim");
            else
                _log.Aviso("OCR não conseguiu extrair texto do PDF");

            return texto;
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao ler PDF localmente: {caminhoPdf}", ex);
            return string.Empty;
        }
    }

    private bool TextoContemCompetencia(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var textoLower = texto.ToLowerInvariant();

        foreach (var mes in MesesPortugues)
        {
            if (textoLower.Contains(mes))
                return true;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(texto, @"\d{1,2}[/\-\.]\d{4}"))
            return true;

        if (System.Text.RegularExpressions.Regex.IsMatch(texto, @"\d{4}"))
            return true;

        return false;
    }

    private async Task<string> LerComItextAsync(string caminhoPdf)
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
            _log.Erro($"Erro ao ler PDF com iText7: {caminhoPdf}", ex);
            return string.Empty;
        }
    }

    private async Task<string> LerComOcrAsync(string caminhoPdf)
    {
        try
        {
            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                Width = 2000
            };

            var texto = new StringBuilder();

            try
            {
                using (var images = new MagickImageCollection(caminhoPdf, settings))
                {
                    foreach (var image in images)
                    {
                        image.Resize(new MagickGeometry(2000, 0) { IgnoreAspectRatio = true });
                        var tempPng = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}.png");
                        try
                        {
                            image.Write(tempPng);

                            using (var engine = new TesseractEngine(@"./tessdata", "por", EngineMode.Default))
                            {
                                using var img = Pix.LoadFromFile(tempPng);
                                using var page = engine.Process(img);
                                texto.Append(page.GetText());
                            }
                        }
                        finally
                        {
                            try { File.Delete(tempPng); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Erro($"Erro ao converter/processar PDF para OCR: {caminhoPdf}", ex);
            }

            return texto.ToString();
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro durante OCR: {caminhoPdf}", ex);
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

        _log.Debug($"Enviando para IA (model: {config.ApiModel}): {textoPdf.Length} chars");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        _log.Debug($"Resposta da API (status: {response.StatusCode}): {jsonResponse}");

        if (!response.IsSuccessStatusCode)
        {
            _log.Erro($"Erro na API OpenRouter: {response.StatusCode} - {jsonResponse}");
            throw new HttpRequestException($"Erro na API: {response.StatusCode} - {jsonResponse}");
        }

        return ParseRespostaIA(jsonResponse);
    }

    private async Task<DocumentoFinanceiro> EnviarParaIAComImagensAsync(string caminhoPdf, AppConfig config)
    {
        var tempImages = new List<string>();

        try
        {
            var settings = new MagickReadSettings
            {
                Density = new Density(200, 200),
                Width = 1500
            };

            _log.Informacao("Convertendo PDF em imagens para IA...");

            using (var images = new MagickImageCollection(caminhoPdf, settings))
            {
                int maxImages = Math.Min(images.Count, 8);

                for (int i = 0; i < maxImages; i++)
                {
                    var tempPng = Path.Combine(Path.GetTempPath(), $"pdfimg_{Guid.NewGuid():N}.jpg");
                    try
                    {
                        var image = images[i];
                        image.Quality = 85;
                        image.Write(tempPng);
                        tempImages.Add(tempPng);
                    }
                    catch (Exception ex)
                    {
                        _log.Erro($"Erro ao converter página {i + 1}: {ex.Message}");
                    }
                }
            }

            if (tempImages.Count == 0)
            {
                _log.Erro("Não foi possível converter nenhuma página do PDF");
                return new DocumentoFinanceiro { Confianca = 0 };
            }

            var imageBases = tempImages.Select(img =>
            {
                var bytes = File.ReadAllBytes(img);
                return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
            }).ToList();

            _log.Debug($"Enviando {imageBases.Count} imagens para IA...");

            var messageContent = new List<object>();
            messageContent.Add(new { type = "text", text = "Analise este documento financeiro escaneado/escrito à mão e extraia as informações:\n\n- Colaborador (beneficiário)\n- Tipo de documento e sigla\n- Competência (mês/ano)\n- Data\n- Número OS\n\nRetorne APENAS um JSON válido." });

            foreach (var imgBase in imageBases)
            {
                messageContent.Add(new { type = "image_url", image_url = new { url = imgBase } });
            }

            var request = new
            {
                model = config.ApiModel,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = messageContent }
                },
                temperature = 0.2,
                max_tokens = 1500
            };

            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://organizador-documentos.local");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Organizador de Documentos");

            _log.Debug($"Enviando para IA (imagens: {imageBases.Count})");

            var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            _log.Debug($"Resposta da API (status: {response.StatusCode}): {jsonResponse}");

            if (!response.IsSuccessStatusCode)
            {
                _log.Erro($"Erro na API OpenRouter: {response.StatusCode} - {jsonResponse}");
                throw new HttpRequestException($"Erro na API: {response.StatusCode} - {jsonResponse}");
            }

            return ParseRespostaIA(jsonResponse);
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao enviar imagens para IA: {ex.Message}", ex);
            return new DocumentoFinanceiro { Confianca = 0 };
        }
        finally
        {
            foreach (var img in tempImages)
            {
                try { File.Delete(img); } catch { }
            }
        }
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

            _log.Debug($"Conteúdo bruto da IA: {content}");

            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (!jsonMatch.Success)
            {
                _log.Aviso($"JSON não encontrado na resposta da IA. Conteúdo: {content}");
                return new DocumentoFinanceiro { Confianca = 0 };
            }

            var dados = JObject.Parse(jsonMatch.Value);
            _log.Debug($"JSON parseado: {dados}");

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
