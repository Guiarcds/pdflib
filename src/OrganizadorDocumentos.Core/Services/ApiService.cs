namespace OrganizadorDocumentos.Core.Services;

using System.IO;
using System.Text;
using ImageMagick;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrganizadorDocumentos.Core.Enums;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;
using Tesseract;

public class ApiService : IApiService
{
    private readonly IConfiguracaoService _configuracao;
    private readonly ILogService _log;
    private readonly HttpClient _httpClient;

private static readonly string SystemPrompt = """
Você é um especialista em documentos financeiros brasileiros.
Analise o PDF fornecido e extraia TODOS os documentos encontrados.
NÃO invente informações. Se não encontrar, retorne array vazio.

Retorne APENAS um JSON válido com a seguinte estrutura (ARRAY de documentos):
[
  {
    "colaborador": "nome completo ou null",
    "tipo_documento": "título/descrição do tipo ou null (OBRIGATÓRIO quando o documento NÃO é recibo)",
    "sigla": "VT|VA|AC|BO|CO|SP|DE|SE|SB|OS|null (null quando o documento NÃO é recibo)",
    "competencia": {
      "mes": 1-12 ou null,
      "ano": YYYY ou null
    },
    "data": "DD/MM/YYYY ou null",
    "numero_os": "número ou null",
    "confianca": 0.0-1.0
  }
]

Siglas: VT=Vale Transporte, VA=Vale Alimentação, AC=Ajuda de Custo,
BO=Bonificação, CO=Comissão, SP=Serviço Prestado, DE=Diária,
SE=Salário Extra, SB=Salário Base, OS=Vale por OS

IMPORTANTE: as siglas acima são SEMPRE de benefícios. Documentos que NÃO são recibos
de benefício (ex.: "Termo de Contrato", "Procuração", "Declaração", "Atestado") NÃO têm
sigla: retorne "sigla": null e "tipo_documento" com o título do documento (regra G).

═══════════════════════════════════════════════════════════════
CLASSIFICAÇÃO DO DOCUMENTO (APLICA-SE A DOCUMENTOS DIGITAIS E ESCANEADOS)
═══════════════════════════════════════════════════════════════
Este sistema NÃO recebe apenas recibos. O PDF pode conter qualquer documento financeiro
ou trabalhista (demonstrativos, relatórios, comprovantes, holerites, listagens, planilhas
impressas, ordens de serviço, etc.). Antes de extrair os dados, classifique cada documento:

- Se for um RECIBO (título "RECIBO" ou estrutura com "Emitente"/"Referente") → aplique as
  REGRAS ESPECIAIS PARA RECIBOS abaixo.
- Se for QUALQUER OUTRO tipo de documento → aplique as REGRAS PARA OUTROS TIPOS DE
  DOCUMENTO (seção mais abaixo).
- As REGRAS PARA EXTRAÇÃO DE COMPETÊNCIA e de COLABORADOR valem para todos os casos.

═══════════════════════════════════════════════════════════════
REGRAS ESPECIAIS PARA RECIBOS MANUSCRITOS / DIGITALIZADOS
═══════════════════════════════════════════════════════════════

0. **PRIMEIRO PASSO OBRIGATÓRIO — IDENTIFICAR E DESCARTAR PÁGINAS EM BRANCO**: 
   Antes de qualquer extração, analise TODAS as páginas/imagens do PDF. Descarte/ignore completamente páginas que:
   - Estejam totalmente em branco (sem texto, sem marcas, sem conteúdo visível)
   - Contenham apenas ruído de digitalização, manchas, riscos aleatórios
   - Não tenham nenhum campo reconhecível (Emitente, Referente, valores, assinatura, texto legível)
   - Sejam páginas de separação, capas vazias, ou versos em branco
   Essas páginas NÃO devem gerar nenhum item no array de retorno. Processe APENAS páginas que contenham recibos/documentos válidos.

1. PROIBIDO INVENTAR DADOS: Nunca crie, complete ou "adivinhe" qualquer
   informação que não esteja claramente legível no documento — isso vale
   para colaborador, sigla, competência, data e número da OS. Se um campo
   não puder ser lido com confiança real, retorne esse campo como null
   em vez de preenchê-lo com um palpite. É sempre preferível reduzir a
   confiança ou mandar o documento para revisão manual do que extrair
   uma informação fabricada.

2. ORIENTAÇÃO DO DOCUMENTO: A página pode estar digitalizada de lado, de cabeça
   para baixo ou girada. Antes de extrair qualquer dado, identifique a orientação
   correta do texto (observando onde estão os campos "Emitente", "Referente",
   valores e assinatura) e leia o conteúdo como se a página estivesse na posição
   correta, independentemente de como a imagem foi digitalizada.

3. COLABORADOR: É o nome escrito no espaço em branco logo após a palavra "Emitente".
   NUNCA confunda com a empresa pagadora — o "Emitente" no recibo é a pessoa que
   está RECEBENDO o pagamento e assinando, ou seja, é o colaborador.

4. TIPO DE PAGAMENTO / SIGLA: Procure o texto escrito após a palavra "Referente".
   Ele pode vir:
   - Por extenso (ex: "Referente: Vale Transporte") → mapeie para a sigla correta (VT, VA, AC, BO, CO, SP, DE, SE, SB, OS)
   - Como sigla solta no campo "nº" próximo ao valor (ex: "nº VT")
   Se não conseguir identificar a sigla com segurança, deixe o campo sigla como
   null e preencha "tipo_documento" com "Recibo" (o título do documento). Documentos com
   "tipo_documento" preenchido ainda são processados automaticamente; somente os que
   não tiverem nem sigla nem tipo vão para revisão manual (REVISAR).

5. COMPETÊNCIA (mês/ano): O mês normalmente aparece escrito por extenso IMEDIATAMENTE
   após o texto de "Referente" (ex: "Referente: Vale Transporte de Setembro").
   O ano costuma ser mais fácil de identificar em outra parte do documento —
   procure o campo de data no padrão "___, DD de MM de AA" (ex: ", 20 de 08 de 26"),
   comum no rodapé ou cabeçalho do recibo. Nesse campo, o ano aparece com 2
   dígitos no final (ex: "26" = 2026) — some 2000 ao valor para obter o ano completo.
   Combine o mês identificado após "Referente" com o ano encontrado nesse
   campo de data.

6. NÚMERO DA OS: Quando o documento for do tipo "OS" (Vale por OS), o número
   NÃO fica em campo separado — ele está embutido no próprio texto após "Referente"
   (ex: "Referente: OS 12345"). Extraia o número de dentro desse texto.

7. PÁGINAS INVÁLIDAS: Se a página estiver em branco, contiver apenas rabiscos,
   riscos aleatórios, ou não for um recibo legível, retorne confiança: 0 e os
   demais campos vazios/nulos. Não tente adivinhar dados de uma página inválida.

8. LETRA MANUSCRITA DIFÍCIL: O nome do colaborador é o campo mais comumente mal
   escrito. Se a caligrafia estiver ambígua a ponto de gerar múltiplas leituras
   plausíveis, prefira reduzir a confiança geral do documento a arriscar um
   palpite incorreto do nome.

═══════════════════════════════════════════════════════════════
REGRAS PARA EXTRAÇÃO DE COMPETÊNCIA (MUITO IMPORTANTE):
═══════════════════════════════════════════════════════════════
- A COMPETÊNCIA é o período de referência do documento (mês/ano)
- Em documentos RECIBO, a competência aparece logo após o nome do benefício, precedida por "Referente" (ex: "Recibo - Referente a 09/2025")
- Procure por datas no formato MM/AAAA, MM/YYYY, DD/MM/AAAA em qualquer parte do documento
- Procure por nomes de meses em português (Janeiro, Fevereiro, Março, Abril, Maio, Junho, Julho, Agosto, Setembro, Outubro, Novembro, Dezembro) seguidos de um ano
- Se encontrar apenas o ano (AAAA) e um número de mês (1-12) em qualquer contexto, use-os como competência
- Se o texto estiver distorcido/ilegível, interprete o melhor possível — não retorne null apenas porque o texto parece ruim
- NÚMEROS QUE PARECEM DATA: "08/08/2026", "20/08/2023", "08-08-2026" são competências válidas
- IGNORE datas de pagamento se houver diferença clara com a data do benefício

═══════════════════════════════════════════════════════════════
REGRAS PARA EXTRAÇÃO DE COLABORADOR (MUITO IMPORTANTE):
═══════════════════════════════════════════════════════════════
- O COLABORADOR é quem **RECEBE** o valor/benefício (beneficiário/titular/favorecido)
- Procure por: "Beneficiário", "Titular", "Funcionário", "Colaborador", "Empregado", "Trabalhador", "Favorecido", "Destinatário"
- **ATENÇÃO COM 'EMITENTE'**: Em RECIBOS, o campo "Emitente" pode indicar QUEM RECEBE (ex: "Emitente: João da Silva"). Nestes casos, USE o nome após "Emitente". Mas em VALES/BOLETOS, "Emitente" é quem EMITE/PAGA (empresa/prefeitura) — NESTES CASOS IGNORE.
- Como distinguir: se o documento tem título "RECIBO" ou "RECIBO DE PAGAMENTO", "Emitente" = colaborador. Se é "VALE", "BOLETO", "COMPROVANTE DE PAGAMENTO", "Emitente" = empresa/pagador (ignore).
- **IGNORE SEMPRE**: nomes de empresas, bancos, órgãos públicos, prefeituras, secretarias, CNPJs como colaborador
- Nomes brasileiros podem ter: acentos (João, São, José), partículas (da, de, do, das, dos), sobrenomes compostos (Silva Santos, Costa Lima)
- Exemplos válidos: "João da Silva", "Maria José dos Santos", "José Maria da Costa Lima"
- Se houver múltiplos nomes, escolha o que aparece como **titular/beneficiário/favorecido/emitente (em recibos)** do documento

═══════════════════════════════════════════════════════════════
REGRAS PARA OUTROS TIPOS DE DOCUMENTO (NÃO-RECIBO)
═══════════════════════════════════════════════════════════════

A) TIPO / SIGLA — o tipo geralmente é dado pelo TÍTULO do documento:
   1º) Leia o título/cabeçalho impresso ou escrito no topo (ex: "DEMONSTRATIVO DE COMISSÕES",
       "RELATÓRIO DE DIÁRIAS", "COMPROVANTE DE VALE TRANSPORTE", "HOLERITE", "ORDEM DE SERVIÇO")
       e mapeie para a sigla correspondente.
   2º) Se o título for genérico ou inexistente (ex: "COMPROVANTE", "DEMONSTRATIVO", "RELATÓRIO",
       sem título), DEDUZA o tipo pelo CONTEXTO do conteúdo — rubricas, descrição dos itens e
       natureza do pagamento:
       - Passagens, recarga de cartão de transporte, deslocamento casa-trabalho → VT
       - Cartão/ticket alimentação ou refeição → VA
       - Reembolso/ressarcimento de despesas, combustível, hospedagem a serviço → AC
       - Prêmio, gratificação, bônus por meta → BO
       - Percentual sobre vendas/serviços, produção → CO
       - Prestação de serviço avulso, autônomo, RPA → SP
       - Valor por dia trabalhado/viajado → DE
       - Horas extras, adicionais, dia extra → SE
       - Salário mensal, remuneração fixa, folha/holerite → SB
       - Pagamento vinculado a número de ordem de serviço → OS
   3º) Se título e conteúdo divergirem, PREVALECE O TÍTULO, mas reduza a confiança.
   4º) Se o documento misturar tipos (ex: holerite com salário + comissão), use o tipo
      principal indicado no título; se não houver, use o de maior valor.
   5º) Se ainda restar dúvida entre dois ou mais tipos → sigla: null e preencha
      "tipo_documento" com o título/descrição como aparece no documento.

B) COLABORADOR — quando não houver rótulo explícito, entenda o CONTEXTO:
   - Nome em destaque no cabeçalho (ex: "Relatório de Diárias — João da Silva")
   - Nome junto à assinatura/ciência de quem RECEBE ("Assinatura do colaborador")
   - Nome na linha/coluna do favorecido em tabelas de pagamento
   - Nome em campos como "Para:", "A/C:", "Nome:", "Prestador:", "Profissional:", "Pago a:"
   - O colaborador é sempre a PESSOA FÍSICA que recebe o valor, nunca a empresa pagadora.
   - Quem assina como responsável pela empresa, contador ou testemunha NÃO é colaborador.
   - Listagens com VÁRIOS colaboradores (ex: folha com 10 nomes): cada colaborador gera um
     item próprio no array, com seu respectivo valor/tipo/competência.

C) COMPETÊNCIA — é o período a que o pagamento SE REFERE, não a data de emissão/pagamento.
   Ordem de busca em documentos não-recibo:
   1º) Rótulos explícitos: "Competência", "Período", "Mês de referência", "Referência", "Ref.",
       "Período de apuração", "Mês/Ano".
   2º) Intervalos de datas (ex: "01/09/2025 a 30/09/2025" → 09/2025). Se cruzar dois meses,
       use o mês em que o período TERMINA, salvo indicação explícita em contrário.
   3º) Contexto: tabelas/relatórios com lançamentos de um mesmo mês → esse é o mês da
       competência. Se houver lançamentos de meses diferentes, use o predominante e reduza
       a confiança.
   4º) Data de emissão apenas como ÚLTIMO recurso para completar mês ou ano, reduzindo a
       confiança. Só assuma "pagamento no mês seguinte" se o próprio documento indicar.
   - Mês e ano podem estar em partes diferentes do documento — combine-os.
   - Se só o mês ou só o ano for identificável, preencha o que for certo e deixe o outro null.

D) CONFIANÇA — reflita a certeza GERAL do documento:
   - 0.9–1.0: título claro, nome legível, competência explícita
   - 0.7–0.89: algum campo deduzido pelo contexto, mas de forma consistente
   - 0.4–0.69: deduções frágeis, caligrafia ambígua ou título/contexto divergentes
   - abaixo de 0.4 ou 0: ilegível, página inválida ou dados insuficientes

E) PÁGINAS DE CONTINUAÇÃO: documentos com várias páginas (ex: holerite de 2 páginas,
   relatório longo) são UM único item no array. Não gere item novo para páginas de
   continuação do mesmo documento.

F) As regras "PROIBIDO INVENTAR DADOS" e "ORIENTAÇÃO DO DOCUMENTO" também valem integralmente
   para estes documentos.

G) DOCUMENTOS SEM SIGLA (NÃO-RECIBO) — REGRA OBRIGATÓRIA:
   Documentos como "TERMO DE CONTRATO", "PROCURAÇÃO", "DECLARAÇÃO", "ATESTADO", "AVISO",
   "ESCALA", "TERMO DE RESPONSABILIDADE" NÃO são recibos de benefício e NÃO possuem sigla.
   Nesses casos:
   - "sigla" DEVE ser null — NUNCA invente uma sigla, NUNCA repita o título no campo sigla
     e NUNCA use "XX", "DOC" ou "RECIBO" como sigla.
   - "tipo_documento" DEVE ser o TÍTULO do documento, exatamente como aparece no papel
     (ex: "Termo de Contrato", "Procuração", "Declaração de Recebimento").
   - A confiança pode ser alta (0.9) quando o título, o colaborador e a competência estão claros.
   Documentos retornados com "sigla": null e "tipo_documento" preenchido são processados
   automaticamente, usando o título como prefixo do nome do arquivo.
   Só omita "tipo_documento" (null) se o documento realmente não tiver título identificável.

IMPORTANTE: Se o PDF contiver MÚLTIPLOS documentos (ex: 15 recibos em um PDF), retorne TODOS no array JSON. Cada item do array é um documento separado.

Retorne APENAS o JSON array, sem explicações adicionais.
""";

    private static readonly string SystemPromptAnaliseEstrutural = """
Você é um especialista em análise de documentos escaneados.

Analise CADA página da imagem fornecida e retorne APENAS um JSON array com as decisões:
[
  {"page": 1, "action": "keep", "rotation": 0},
  {"page": 2, "action": "discard", "reason": "blank"},
  {"page": 3, "action": "rotate", "rotation": 180}
]

Regras:
- action: "keep" | "discard" | "rotate"
- rotation: 0, 90, 180, 270 (graus para corrigir orientação)
- Se página em branco/ruído → "discard"
- Se texto de cabeça para baixo/lado → "rotate" com graus necessários
- Se OK → "keep" com rotation: 0
""";

    public ApiService(IConfiguracaoService configuracao, ILogService log, HttpClient? httpClient = null)
    {
        _configuracao = configuracao;
        _log = log;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    public async Task<List<DocumentoFinanceiro>> ExtrairDadosAsync(string caminhoPdf)
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

    public async Task<List<PageDecision>> AnalisarEstruturaPdfAsync(string caminhoPdf)
    {
        var config = _configuracao.ObterConfiguracao();

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("API Key não configurada");
        }

        _log.Informacao($"Analisando estrutura do PDF: {Path.GetFileName(caminhoPdf)}");

        var tempImages = new List<string>();

        try
        {
            var settings = new MagickReadSettings
            {
                Density = new Density(200, 200),
                Width = 1500
            };

            using var images = new MagickImageCollection(caminhoPdf, settings);
            int totalPaginas = images.Count;
            _log.Informacao($"PDF possui {totalPaginas} página(s)");

            var todasDecisoes = new List<PageDecision>();
            const int tamanhoChunk = 10;

            for (int start = 0; start < totalPaginas; start += tamanhoChunk)
            {
                int count = Math.Min(tamanhoChunk, totalPaginas - start);
                var imageBases = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    var image = images[start + i];
                    image.Quality = 85;
                    var tempPng = Path.Combine(Path.GetTempPath(), $"analise_{Guid.NewGuid():N}.jpg");
                    image.Write(tempPng);
                    tempImages.Add(tempPng);

                    var bytes = await File.ReadAllBytesAsync(tempPng);
                    imageBases.Add($"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}");
                }

                _log.Debug($"Enviando chunk de {imageBases.Count} página(s) para análise estrutural (índice inicial: {start + 1})");

                var decisoes = await EnviarDecisoesParaIAAsync(imageBases, config);

                for (int j = 0; j < decisoes.Count; j++)
                {
                    decisoes[j].Page += start;
                }

                todasDecisoes.AddRange(decisoes);
            }

            for (int i = 1; i <= totalPaginas; i++)
            {
                if (!todasDecisoes.Any(d => d.Page == i))
                {
                    _log.Aviso($"Página {i} sem decisão da IA, usando fallback 'keep'");
                    todasDecisoes.Add(new PageDecision { Page = i, Action = PageAction.Keep, Rotation = 0 });
                }
            }

            return todasDecisoes.OrderBy(d => d.Page).ToList();
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao analisar estrutura do PDF: {caminhoPdf}", ex);
            throw;
        }
        finally
        {
            foreach (var img in tempImages)
            {
                try { File.Delete(img); } catch { }
            }
        }
    }

    public async Task<DocumentoFinanceiro> ExtrairDadosDocumentoAsync(string caminhoPdf)
    {
        var config = _configuracao.ObterConfiguracao();

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("API Key não configurada");
        }

        _log.Informacao($"Extraindo dados do documento (1 página): {Path.GetFileName(caminhoPdf)}");

        var documentos = await EnviarParaIAComImagensAsync(caminhoPdf, config);
        return documentos.FirstOrDefault() ?? new DocumentoFinanceiro { Confianca = 0 };
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

    private async Task<List<DocumentoFinanceiro>> EnviarParaIAAsync(string textoPdf, AppConfig config)
    {
        var request = new
        {
            model = config.ApiModel,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Analise este documento financeiro e extraia TODOS os documentos encontrados (pode haver múltiplos em um PDF):\n\n{textoPdf}" }
            },
            temperature = 0.1,
            max_tokens = 3000
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

        return ParseRespostaIAArray(jsonResponse);
    }

    private async Task<List<DocumentoFinanceiro>> EnviarParaIAComImagensAsync(string caminhoPdf, AppConfig config)
    {
        var tempImages = new List<string>();
        var imagensValidas = new List<string>();

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
                        
                        // 1. Auto-orientar baseado em EXIF/orientação
                        image.AutoOrient();
                        
                        // 2. Verificar se página está em branco/quase em branco
                        if (EhPaginaEmBranco(image))
                        {
                            _log.Informacao($"Página {i + 1} detectada como em branco/ruído, descartando");
                            continue;
                        }

                        image.Quality = 85;
                        image.Write(tempPng);
                        tempImages.Add(tempPng);
                        imagensValidas.Add(tempPng);
                        
                        _log.Debug($"Página {i + 1} processada e válida");
                    }
                    catch (Exception ex)
                    {
                        _log.Erro($"Erro ao converter página {i + 1}: {ex.Message}");
                    }
                }
            }

            if (imagensValidas.Count == 0)
            {
                _log.Aviso("Nenhuma página válida encontrada no PDF (todas em branco/ruído)");
                return new List<DocumentoFinanceiro> { new DocumentoFinanceiro { Confianca = 0 } };
            }

            _log.Informacao($"Enviando {imagensValidas.Count} página(s) válida(s) para IA (de {tempImages.Count} total)");

            var imageBases = imagensValidas.Select(img =>
            {
                var bytes = File.ReadAllBytes(img);
                return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
            }).ToList();

            var messageContent = new List<object>();
            messageContent.Add(new { type = "text", text = """
Analise este documento financeiro escaneado/escrito à mão e extraia TODOS os documentos encontrados (pode haver múltiplos em um PDF).

⚠️ IMPORTANTE — Páginas em branco/ruído JÁ FORAM REMOVIDAS localmente. As imagens abaixo contêm APENAS páginas com conteúdo legível.

REGRAS DE EXTRAÇÃO:

1. ORIENTAÇÃO: As imagens já foram auto-rotacionadas. Se ainda houver texto de lado, ajuste mentalmente.

2. COLABORADOR: É o nome após "Emitente" (quem RECEBE/assina). NÃO confunda com empresa pagadora.

3. SIGLA: Procure após "Referente". Pode vir por extenso ("Vale Transporte") → VT, VA, AC, BO, CO, SP, DE, SE, SB, OS. Se houver "nº VT" próximo ao valor, use essa sigla. Se incerto → null.

4. COMPETÊNCIA: Mês (por extenso) logo após "Referente" (ex: "Referente: Vale Transporte de Setembro"). Ano em outra parte (cabeçalho, assinatura).

5. NÚMERO OS: Se for tipo OS, está embutido em "Referente" (ex: "Referente: OS 12345").

6. LETRA DIFÍCIL: Se nome ambíguo → reduza confiança, não chute.

7. OUTROS TIPOS DE DOCUMENTO: o PDF pode conter documentos que NÃO são recibos (demonstrativos,
   relatórios, comprovantes, holerites, listagens). Nesses casos, identifique o TIPO pelo
   título e, se for genérico, pelo contexto do conteúdo; identifique o COLABORADOR (pessoa
   que recebe) e a COMPETÊNCIA (período de referência) entendendo o contexto do documento,
   mesmo sem os campos "Emitente"/"Referente".

8. DOCUMENTO SEM SIGLA (NÃO-RECIBO): se o título NÃO corresponde a nenhum benefício
   (VT, VA, AC, BO, CO, SP, DE, SE, SB, OS) — ex.: "Termo de Contrato", "Procuração",
   "Declaração" — retorne "sigla": null (NUNCA invente uma sigla) e "tipo_documento" com o
   TÍTULO exato do documento. Esses documentos são processados automaticamente pelo título.

Retorne APENAS um JSON array válido com:
[
  {
    "colaborador": "nome ou null",
    "tipo_documento": "título/descrição ou null (OBRIGATÓRIO quando o documento NÃO é recibo)",
    "sigla": "VT|VA|AC|BO|CO|SP|DE|SE|SB|OS|null (null quando o documento NÃO é recibo)",
    "competencia": { "mes": 1-12 ou null, "ano": YYYY ou null },
    "data": "DD/MM/YYYY ou null",
    "numero_os": "número ou null",
    "confianca": 0.0-1.0
  }
]
""" });

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
                max_tokens = 4000
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

            return ParseRespostaIAArray(jsonResponse);
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao enviar imagens para IA: {ex.Message}", ex);
            return new List<DocumentoFinanceiro> { new DocumentoFinanceiro { Confianca = 0 } };
        }
        finally
        {
            foreach (var img in tempImages)
            {
                try { File.Delete(img); } catch { }
            }
        }
    }

    private List<DocumentoFinanceiro> ParseRespostaIAArray(string jsonResponse)
    {
        try
        {
            var jsonObject = JObject.Parse(jsonResponse);
            var content = jsonObject["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(content))
            {
                _log.Aviso("Resposta da IA vazia");
                return new List<DocumentoFinanceiro> { new DocumentoFinanceiro { Confianca = 0 } };
            }

            _log.Debug($"Conteúdo bruto da IA: {content}");

            // Tenta encontrar array JSON na resposta
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\[.*\]", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (!jsonMatch.Success)
            {
                // Fallback: tenta encontrar objeto único e transforma em array
                var objectMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (objectMatch.Success)
                {
                    _log.Debug("Resposta não é array, tentando parsear como objeto único");
                    var dados = JObject.Parse(objectMatch.Value);
                    return new List<DocumentoFinanceiro> { ParseDocumentoFromJson(dados) };
                }
                _log.Aviso($"JSON array não encontrado na resposta da IA. Conteúdo: {content}");
                return new List<DocumentoFinanceiro> { new DocumentoFinanceiro { Confianca = 0 } };
            }

            var array = JArray.Parse(jsonMatch.Value);
            _log.Debug($"JSON array parseado com {array.Count} itens");

            var documentos = new List<DocumentoFinanceiro>();
            foreach (var item in array)
            {
                if (item is JObject obj)
                {
                    var doc = ParseDocumentoFromJson(obj);
                    documentos.Add(doc);
                }
            }

            if (documentos.Count == 0)
            {
                _log.Aviso("Array JSON vazio ou sem itens válidos");
                return new List<DocumentoFinanceiro> { new DocumentoFinanceiro { Confianca = 0 } };
            }

            return documentos;
        }
        catch (Exception ex)
        {
            _log.Erro("Erro ao parsear resposta da IA", ex);
            return new List<DocumentoFinanceiro> { new DocumentoFinanceiro { Confianca = 0 } };
        }
    }

    private DocumentoFinanceiro ParseDocumentoFromJson(JObject dados)
    {
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

    private async Task<List<PageDecision>> EnviarDecisoesParaIAAsync(List<string> imageBases, AppConfig config)
    {
        var messageContent = new List<object>();
        messageContent.Add(new { type = "text", text = """
Analise CADA página deste PDF e retorne APENAS um JSON array com as decisões:
[
  {"page": 1, "action": "keep", "rotation": 0},
  {"page": 2, "action": "discard", "reason": "blank"},
  {"page": 3, "action": "rotate", "rotation": 180}
]

Regras:
- action: "keep" | "discard" | "rotate"
- rotation: 0, 90, 180, 270 (graus para corrigir orientação)
- Se página em branco/ruído → "discard"
- Se texto de cabeça para baixo/lado → "rotate" com graus necessários
- Se OK → "keep" com rotation: 0
""" });

        foreach (var imgBase in imageBases)
        {
            messageContent.Add(new { type = "image_url", image_url = new { url = imgBase } });
        }

        var request = new
        {
            model = config.ApiModel,
            messages = new object[]
            {
                new { role = "system", content = SystemPromptAnaliseEstrutural },
                new { role = "user", content = messageContent }
            },
            temperature = 0.1,
            max_tokens = 3000
        };

        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://organizador-documentos.local");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Organizador de Documentos");

        _log.Debug($"Enviando para IA (imagens: {imageBases.Count}) análise estrutural");

        var response = await _httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        _log.Debug($"Resposta da API (status: {response.StatusCode}): {jsonResponse}");

        if (!response.IsSuccessStatusCode)
        {
            _log.Erro($"Erro na API OpenRouter: {response.StatusCode} - {jsonResponse}");
            throw new HttpRequestException($"Erro na API: {response.StatusCode} - {jsonResponse}");
        }

        return ParseRespostaIADecisoes(jsonResponse);
    }

    private List<PageDecision> ParseRespostaIADecisoes(string jsonResponse)
    {
        try
        {
            var jsonObject = JObject.Parse(jsonResponse);
            var content = jsonObject["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(content))
            {
                _log.Aviso("Resposta da IA vazia para análise estrutural");
                return new List<PageDecision>();
            }

            _log.Debug($"Conteúdo bruto da IA (análise): {content}");

            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\[.*\]", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (!jsonMatch.Success)
            {
                _log.Aviso($"JSON array não encontrado na resposta da IA (análise). Conteúdo: {content}");
                return new List<PageDecision>();
            }

            var array = JArray.Parse(jsonMatch.Value);
            _log.Debug($"JSON array parseado com {array.Count} decisões");

            var decisoes = new List<PageDecision>();
            foreach (var item in array)
            {
                if (item is JObject obj)
                {
                    var actionStr = obj["action"]?.ToString() ?? "keep";
                    var page = obj["page"]?.ToObject<int>() ?? 0;
                    var rotation = obj["rotation"]?.ToObject<int>() ?? 0;

                    var action = actionStr.ToLowerInvariant() switch
                    {
                        "discard" => PageAction.Discard,
                        "rotate" => PageAction.Rotate,
                        _ => PageAction.Keep
                    };

                    decisoes.Add(new PageDecision
                    {
                        Page = page,
                        Action = action,
                        Rotation = rotation,
                        Reason = obj["reason"]?.ToString()
                    });
                }
            }

            return decisoes;
        }
        catch (Exception ex)
        {
            _log.Erro("Erro ao parsear decisões da IA", ex);
            return new List<PageDecision>();
        }
    }

    private bool EhPaginaEmBranco(IMagickImage image)
    {
        try
        {
            // Cria cópia para análise via stream (evita problema de construtor)
            using var memStream = new MemoryStream();
            image.Write(memStream, MagickFormat.Png);
            memStream.Position = 0;
            
            using var grayImage = new MagickImage(memStream);
            grayImage.ColorSpace = ColorSpace.Gray;
            
            // Calcula estatísticas da imagem
            var stats = grayImage.Statistics();
            var composite = stats.GetChannel(PixelChannel.Composite);
            
            // Critérios para página em branco:
            // 1. Desvio padrão muito baixo (pouca variação = página uniforme)
            // 2. Média muito alta (quase todo branco)
            // 3. Ou média muito baixa + desvio baixo (preto sólido - raro mas possível)
            
            double mean = composite.Mean;
            double stdDev = composite.StandardDeviation;
            
            // Normaliza para 0-1 se necessário (Magick.NET usa quantum depth)
            double maxValue = grayImage.Depth == 8 ? 255.0 : 65535.0;
            double meanNorm = mean / maxValue;
            double stdDevNorm = stdDev / maxValue;
            
            // Página em branco: média > 95% (quase branco) E desvio < 5% (muito uniforme)
            bool emBranco = meanNorm > 0.95 && stdDevNorm < 0.05;
            
            // Página preta sólida (raro): média < 5% E desvio < 5%
            bool pretaSolida = meanNorm < 0.05 && stdDevNorm < 0.05;
            
            // Ruído apenas: desvio muito baixo mas média no meio (cinza uniforme)
            bool ruidaoUniforme = stdDevNorm < 0.02 && meanNorm > 0.3 && meanNorm < 0.7;
            
            if (emBranco || pretaSolida || ruidaoUniforme)
            {
                _log.Debug($"Página descartada: mean={meanNorm:P2}, stdDev={stdDevNorm:P2}, branco={emBranco}, preta={pretaSolida}, ruído={ruidaoUniforme}");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _log.Aviso($"Erro ao analisar página em branco: {ex.Message}");
            return false; // Em caso de erro, não descarta
        }
    }
}