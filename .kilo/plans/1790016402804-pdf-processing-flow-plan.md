# Plano: Novo Fluxo de Processamento de PDFs

## Objetivo
Reorganizar o fluxo de processamento para que a **IA analise primeiro** (identificar páginas em branco, orientação, tipo de documento) e o **Magick.NET aplique correções** antes da extração final de dados.

---

## Fluxo Novo (4 Fases)

```
PDF Original
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ FASE 1 — IA: ANÁLISE ESTRUTURAL (TODAS as páginas)          │
│ • Envia TODAS as páginas do PDF para IA (sem limite 8)      │
│ • IA retorna: array de decisões por página                  │
│   [{page: 1, action: 'keep', rotation: 0},                  │
│    {page: 2, action: 'discard', reason: 'blank'},           │
│    {page: 3, action: 'rotate', rotation: 180}]              │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ FASE 2 — MAGICK.NET: APLICAÇÃO DE CORREÇÕES                  │
│ • Para cada página:                                         │
│   - action='discard' → pula                                 │
│   - action='rotate' → image.Rotate(degrees)                 │
│   - action='keep' → mantém                                  │
│ • Salva imagens corrigidas em temp                          │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ FASE 3 — SPLIT RECIBO (1 página = 1 documento)              │
│ • Cada página 'keep' → 1 PDF separado                       │
│ • Naming: REC_{páginaOriginal}_{timestamp}.pdf              │
│ • Salva em pasta temp/split/                                │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│ FASE 4 — IA: EXTRAÇÃO DE DADOS (por documento)              │
│ • Para cada PDF de 1 página:                                │
│   - Envia imagem para IA com prompt de extração             │
│   - Retorna DocumentoFinanceiro (colaborador, sigla, etc.)  │
│ • ProcessamentoService organiza/renomeia/move               │
└─────────────────────────────────────────────────────────────┘
```

---

## Alterações Necessárias

### 1. `IApiService` — Nova interface
```csharp
public interface IApiService
{
    // Fase 1: Análise estrutural
    Task<List<PageDecision>> AnalisarEstruturaPdfAsync(string caminhoPdf);
    
    // Fase 4: Extração de dados por documento
    Task<DocumentoFinanceiro> ExtrairDadosDocumentoAsync(string caminhoPdf);
}
```

### 2. `ApiService` — Implementação
- **`AnalisarEstruturaPdfAsync`**: 
  - Converte TODAS as páginas (sem limite) → base64
  - Prompt Fase 1: "Analise cada página e retorne decisões"
  - Parse array `[{page, action, rotation, reason?}]`
  
- **`ExtrairDadosDocumentoAsync`**:
  - Recebe PDF de 1 página
  - Prompt Fase 4: extração completa (já existe, adaptar)

### 3. `IFileService` + `FileService` — Novos métodos
```csharp
// Aplica rotações + descarta + salva imagens corrigidas
Task<List<string>> AplicarCorrecoesAsync(string caminhoPdf, List<PageDecision> decisoes, string pastaTemp);

// Split: cada página → PDF individual
Task<List<string>> SplitUmaPaginaPorPdfAsync(List<string> imagensCorrigidas, string pastaTemp);
```

### 4. `ProcessamentoService` — Orquestrador novo
```csharp
public async Task<List<ResultadoProcessamento>> ProcessarPdfCompletoAsync(string caminhoPdf)
{
    // 1. IA analisa estrutura
    var decisoes = await _apiService.AnalisarEstruturaPdfAsync(caminhoPdf);
    
    // 2. Magick.NET aplica correções
    var imagensCorrigidas = await _fileService.AplicarCorrecoesAsync(caminhoPdf, decisoes, pastaTemp);
    
    // 3. Split RECIBO (1 pág = 1 doc)
    var pdfsIndividuais = await _fileService.SplitUmaPaginaPorPdfAsync(imagensCorrigidas, pastaTemp);
    
    // 4. Para cada PDF: extrai dados + organiza
    var resultados = new List<ResultadoProcessamento>();
    foreach (var pdf in pdfsIndividuais)
    {
        var dados = await _apiService.ExtrairDadosDocumentoAsync(pdf);
        var resultado = await ProcessarDocumentoIndividualAsync(pdf, dados);
        resultados.Add(resultado);
    }
    return resultados;
}
```

### 5. Novo Model: `PageDecision`
```csharp
public class PageDecision
{
    public int Page { get; set; }           // 1-based
    public PageAction Action { get; set; }  // Keep, Discard, Rotate
    public int Rotation { get; set; }       // 0, 90, 180, 270
    public string? Reason { get; set; }     // "blank", "noise", etc.
}

public enum PageAction { Keep, Discard, Rotate }
```

### 6. Prompts IA

**Fase 1 (Análise Estrutural):**
```
Analise CADA página deste PDF e retorne APENAS um JSON array:
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
```

**Fase 4 (Extração) — já existe, manter.**

---

## Compatibilidade / Migração

- `ProcessarDocumentoAsync` (interface atual) → delega para `ProcessarPdfCompletoAsync` e retorna primeiro resultado
- `ProcessarLoteAsync` → usa novo fluxo para cada arquivo
- Testes existentes: adaptar mocks para nova interface

---

## Riscos / Pontos de Atenção

| Risco | Mitigação |
|-------|-----------|
| IA Fase 1 pode errar rotação | Log detalhado; fallback para AutoOrient() local se IA falhar |
| PDFs com >20 páginas (tokens) | Chunking: processa em lotes de 10 páginas |
| Custo 2 chamadas IA por PDF | Fase 1 usa modelo barato (Flash); Fase 4 só páginas válidas |
| Temp files cleanup | `try/finally` rigoroso em FileService |

---

## Validação

1. **Unit tests**: `ApiServiceTests.AnalisarEstruturaPdf_ReturnsDecisions`
2. **Integration test**: PDF com 3 páginas (1 blank, 1 rotated, 1 OK) → 2 documentos finais
3. **Manual**: PDF real com 15 RECIBOs → 15 PDFs organizados em pastas corretas