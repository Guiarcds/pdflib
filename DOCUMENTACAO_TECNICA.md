# 📘 Documentação Técnica — Organizador de Documentos Financeiros

> **Documentação técnica para desenvolvedores** — Referência completa de cada pasta, arquivo, classe e função do projeto.

**Projeto:** OrganizadorDocumentos  
**Versão:** 1.0  
**Framework:** .NET 10 (C# 13)  
**Padrão:** MVVM + Injeção de Dependência  
**Data:** Setembro/2026

---

## 📋 Índice

1. [Visão Geral da Arquitetura](#1-visão-geral-da-arquitetura)
2. [Estrutura de Diretórios](#2-estrutura-de-diretórios)
3. [Solution e Projetos](#3-solution-e-projetos)
4. [Scripts e Arquivos de Execução](#4-scripts-e-arquivos-de-execução)
5. [Pasta `src/` — Código-Fonte](#5-pasta-src—código-fonte)
6. [Pasta `tests/` — Testes Automatizados](#6-pasta-tests—testes-automatizados)
7. [Pasta `bin/` — Artefatos de Build](#7-pasta-bin—artefatos-de-build)
8. [Fluxo de Dados do Sistema](#8-fluxo-de-dados-do-sistema)

---

## 1. Visão Geral da Arquitetura

O sistema é um **aplicativo WPF** que organiza automaticamente PDFs financeiros brasileiros (Vale Transporte, Vale Alimentação, etc.) em uma estrutura de pastas por colaborador/ano/mês.

```
┌─────────────────────────────────────────────────────────────────┐
│                     ORGANIZADOR DE DOCUMENTOS                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│   │   INTERFACE  │    │   CÉREBRO    │    │  ARMAZENAMENTO│     │
│   │   (WPF UI)   │◄──►│  (Core/Serviços)│◄─►│  (Pastas/JSON)│     │
│   └──────────────┘    └──────────────┘    └──────────────┘     │
│         ▲                   ▲                   ▲                │
│         │                   │                   │                │
│   Botões, Listas,      IA + Regras         Config.json,       │
│   Formulários          de Negócio         Logs, Pastas         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### As 3 Camadas Principais

| Camada | O que faz | Analogia |
|--------|-----------|----------|
| **Interface (UI)** | Tela que você vê e clica | Painel de controle de uma fábrica |
| **Core (Serviços)** | Regras, IA, organização | Gerente da fábrica |
| **Dados (Armazenamento)** | Configurações e arquivos | Arquivo morto + prateleiras |

---

## 2. Estrutura de Diretórios

```
C:\Guilherme\PDFlib\pdflib\
├── .gitignore                    # Ignorar build artifacts no Git
├── .vscode/                      # Configurações do VS Code (c_cpp_properties, launch, settings)
├── .kilo/                        # Configuração do agente Kilo (plans, gitignore)
├── DOCUMENTACAO.md               # Documentação didática (não técnica)
├── DOCUMENTACAO_TECNICA.md       # ← ESTE ARQUIVO
├── README.md                     # Guia rápido de uso
├── Iniciar.bat                   # Script para iniciar o app
├── Compilar e Iniciar.bat        # Script para compilar e iniciar
├── Terminal RUN-README.txt       # Script PowerShell para .gitignore
├── OrganizadorDocumentos.slnx    # Solution file (em src/)
├── windowsdesktop-runtime-10.0.11-win-x64.exe  # Instalador do runtime .NET 10
├── bin/                          # Artefatos compilados (.exe, .dll)
├── src/                          # Código-fonte principal
│   ├── OrganizadorDocumentos.slnx
│   ├── OrganizadorDocumentos.Core/
│   │   ├── OrganizadorDocumentos.Core.csproj
│   │   ├── config.example.json
│   │   ├── Class1.cs (placeholder)
│   │   ├── Models/
│   │   ├── Enums/
│   │   ├── Constants/
│   │   └── Services/
│   ├── OrganizadorDocumentos.Data/
│   │   ├── OrganizadorDocumentos.Data.csproj
│   │   └── Class1.cs (placeholder)
│   └── OrganizadorDocumentos.UI/
│       ├── OrganizadorDocumentos.UI.csproj
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── ViewModels/
│       ├── Views/
│       └── Converters/
└── tests/
    └── OrganizadorDocumentos.Tests/
        ├── OrganizadorDocumentos.Tests.csproj
        ├── FileServiceTests.cs
        └── NormalizacaoServiceTests.cs
```

---

## 3. Solution e Projetos

### 3.1 `src/OrganizadorDocumentos.slnx`

Arquivo de solution que referencia 4 projetos:

```xml
<Project Path="../tests/OrganizadorDocumentos.Tests/OrganizadorDocumentos.Tests.csproj" />
<Project Path="OrganizadorDocumentos.Core/OrganizadorDocumentos.Core.csproj" />
<Project Path="OrganizadorDocumentos.Data/OrganizadorDocumentos.Data.csproj" />
<Project Path="OrganizadorDocumentos.UI/OrganizadorDocumentos.UI.csproj" />
```

### 3.2 `src/OrganizadorDocumentos.Core/OrganizadorDocumentos.Core.csproj`

**Tipo:** Class Library (NuGet)  
**Target:** net10.0  
**Função:** Camada principal com toda a lógica de negócio, modelos, serviços e regras.

**Dependências NuGet:**
| Pacote | Versão | Finalidade |
|--------|--------|------------|
| FuzzySharp | 2.0.2 | Comparação fuzzy de nomes |
| itext7 | 9.7.0 | Extração de texto de PDFs |
| Magick.NET-Q16-AnyCPU | 13.1.0 | Renderizar PDF como imagem para OCR |
| Microsoft.Extensions.DependencyInjection | 10.0.11 | Container DI |
| Newtonsoft.Json | 13.0.4 | Serialização JSON |
| Serilog | 4.4.0 | Framework de logging |
| Serilog.Sinks.File | 7.0.0 | Logs em arquivo |
| Tesseract | 5.2.0 | Reconhecimento óptico de caracteres (OCR) |

**Dependência:** Referencia OrganizadorDocumentos.Data

### 3.3 `src/OrganizadorDocumentos.Data/OrganizadorDocumentos.Data.csproj`

**Tipo:** Class Library (lightweight)  
**Target:** net10.0  
**Função:** Camada de acesso a dados (atualmente vazio — placeholder para expansão futura). Nenhuma dependência externa.

### 3.4 `src/OrganizadorDocumentos.UI/OrganizadorDocumentos.UI.csproj`

**Tipo:** WPF Executable (WinExe)  
**Target:** net10.0-windows  
**Função:** Aplicação WPF com interface gráfica.

**Propriedades especiais:**
- `UseWPF=true` — Habilita WPF
- `OutputPath=..\..\bin\` — O exe vai direto para a pasta bin/ raiz
- **PostBuild:** Copia o .exe para `..\..\bin\` automaticamente

**Dependências:** Referencia Core e Data

---

## 4. Scripts e Arquivos de Execução

### 4.1 `Iniciar.bat`

**Função:** Executa o aplicativo já compilado.

**Fluxo:**
1. Verifica se o .NET SDK está instalado (`where dotnet`)
2. Se não estiver, tenta instalar via `dotnet-sdk-10.0.400-win-x64.exe` presente na mesma pasta
3. Verifica se `bin\OrganizadorDocumentos.UI.exe` existe
4. Se não existir, exibe mensagem de erro pedindo para compilar primeiro
5. Se tudo OK, executa `start "" "bin\OrganizadorDocumentos.UI.exe"`

### 4.2 `Compilar e Iniciar.bat`

**Função:** Compila o projeto e então inicia o aplicativo.

**Fluxo:**
1. Verifica/instala o .NET SDK (igual ao Iniciar.bat)
2. Executa `dotnet build src\OrganizadorDocumentos.slnx --configuration Release`
3. Se a compilação falhar, exibe erro e para
4. Se sucesso, inicia `bin\OrganizadorDocumentos.UI.exe`

### 4.3 `Terminal RUN-README.txt`

**Função:** Script PowerShell para manutenção do `.gitignore`. Contém 3 operações:
1. Reescreve o `.gitignore` como UTF-8 sem BOM com a lista completa de pastas/arquivos a ignorar
2. Remove artefatos de build rastreados pelo Git (`git rm -r --cached`)
3. Faz commit das alterações

### 4.4 `.gitignore`

Ignora todos os artefatos de build do .NET: `bin/`, `obj/`, `.dll`, `.exe`, `.pdb`, `.cache`, arquivos de assets, `.g.cs`, `.baml`, `.runtimeconfig.json`, `.deps.json`, `.AssemblyInfo.cs`, `.Up2Date`, e o próprio `Terminal RUN-README.txt`.

### 4.5 `README.md`

Guia rápido de uso: como iniciar, configurar, mapear pastas e processar PDFs. Inclui tabela de siglas de documentos (VT, VA, AC, etc.) e padrão de nomenclatura de arquivos.

### 4.6 `DOCUMENTACAO.md`

Documentação didática para não desenvolvedores — explica o sistema com analogias, diagramas e glossário.

---

## 5. Pasta `src/` — Código-Fonte

### 5.1 `src/OrganizadorDocumentos.Core/` — Camada de Negócio Principal

Contém todos os modelos (Models), enums, constantes, interfaces e serviços que implementam a lógica central do sistema.

#### 5.1.1 `OrganizadorDocumentos.Core.csproj`

Arquivo de projeto da camada Core. Define dependências NuGet (FuzzySharp, itext7, Serilog, Newtonsoft.Json, DI) e referencia o projeto Data.

**Dependências NuGet:**
| Pacote | Versão | Finalidade |
|--------|--------|------------|
| FuzzySharp | 2.0.2 | Comparação fuzzy de nomes |
| itext7 | 9.7.0 | Extração de texto de PDFs |
| Magick.NET-Q16-AnyCPU | 13.1.0 | Renderizar PDF como imagem para OCR |
| Microsoft.Extensions.DependencyInjection | 10.0.11 | Container DI |
| Newtonsoft.Json | 13.0.4 | Serialização JSON |
| Serilog | 4.4.0 | Framework de logging |
| Serilog.Sinks.File | 7.0.0 | Logs em arquivo |
| Tesseract | 5.2.0 | Reconhecimento óptico de caracteres (OCR) |

**Dependência:** Referencia OrganizadorDocumentos.Data

#### 5.1.2 `config.example.json`

Template de configuração com valores padrão:

| Campo | Valor Padrão | Descrição |
|-------|-------------|-----------|
| PastaRaiz | "" | Diretório raiz do sistema |
| PastaColaboradores | "COLABORADORES" | Pasta de colaboradores |
| PastaRevisar | "REVISAR" | Pasta de documentos para revisão |
| PastaEntrada | "ENTRADA" | Pasta de PDFs novos |
| ApiKey | "" | Chave da API OpenRouter |
| ApiModel | "google/gemini-2.0-flash-001" | Modelo IA utilizado |
| ModoOperacao | "seguro" | Modo de operação |
| LimiarFuzzy | 0.80 | Limiar de similaridade |
| IntervaloVarreduraMinutos | 30 | Intervalo de verificação |
| MascaraMes | "00 - Mmmm" | Formato dos nomes de mês |

#### 5.1.3 `Class1.cs`

Placeholder vazio — restante de scaffolding do projeto. Sem funcionalidade.

---

### 5.2 `src/OrganizadorDocumentos.Core/Models/` — Modelos de Dados

#### 5.2.1 `ResultadoProcessamento.cs`

Representa o resultado do processamento de um PDF.
- `ArquivoOrigem` (string?) — Caminho do arquivo original
- `CaminhoDestino` (string?) — Caminho onde foi movido
- `Status` (StatusProcessamento) — Sucesso / Revisar / Erro
- `Mensagem` (string?) — Descrição do resultado
- `DadosExtraidos` (DocumentoFinanceiro?) — Dados extraídos pela IA
- `ProcessadoEm` (DateTime) — Timestamp do processamento

#### 5.2.2 `DocumentoFinanceiro.cs`

Dados extraídos de um PDF financeiro pela IA.
- `Colaborador` — Nome do beneficiário
- `TipoDocumento` — Descrição do tipo (Vale Transporte, etc.)
- `Sigla` — Sigla do documento (VT, VA, AC, etc.)
- `Competencia` — Mês/ano do documento
- `Data` — Data do documento
- `NumeroOS` — Número da OS (se aplicável)
- `Confianca` (double) — Score de confiança da IA (0.0-1.0)

**Propriedades computadas:** `TemColaborador`, `TemSigla`, `TemCompetencia`

#### 5.2.3 `Competencia.cs`

Representa mês/ano de competência.
- `Mes` (int?) — Número do mês (1-12)
- `Ano` (int?) — Ano
- `Completa` → true se ambos definidos
- `ToString()` → Formato "MM-AAAA"

#### 5.2.4 `Colaborador.cs`

Representa um colaborador (pasta).
- `NomePasta` — Nome da pasta (ex: "JOAO_DA_SILVA")
- `NomeNormalizado` — Nome sem acentos/partículas (para fuzzy matching)
- `CaminhoCompleto` — Caminho absoluto no disco
- `Anos` (List<PastaAno>) — Anos do colaborador

#### 5.2.5 `PastaAno.cs`

Representa uma pasta de ano dentro de um colaborador.
- `Ano` (int) — Ano (ex: 2025)
- `NomePasta` — Nome da pasta
- `CaminhoCompleto` — Caminho absoluto
- `Meses` (List<PastaMes>) — Pastas mensais

#### 5.2.6 `PastaMes.cs`

Representa uma pasta de mês dentro de um ano.
- `NumeroMes` (int) — Número do mês (1-12)
- `NomePasta` — Nome da pasta (ex: "05 - Maio")
- `NomeNormalizado` — Nome normalizado
- `CaminhoCompleto` — Caminho absoluto

#### 5.2.7 `EstruturaPasta.cs`

Root aggregate — Representa a árvore completa de pastas.
- `Colaboradores` (List<Colaborador>) — Todos os colaboradores
- `TotalAnos` → Total de anos em todos os colaboradores
- `TotalPastasMensais` → Total de pastas mensais
- `TotalColaboradores` → Número de colaboradores

---

### 5.3 `src/OrganizadorDocumentos.Core/Enums/` — Enumerações

#### 5.3.1 `TipoDocumento.cs`

Enum com as siglas de documentos financeiros brasileiros:
VT (Vale Transporte), VA (Vale Alimentação), AC (Ajuda de Custo), BO (Bonificação), CO (Comissão), SP (Serviço Prestado), DE (Diária), SE (Salário Extra), SB (Salário Base), OS (Vale por OS)

#### 5.3.2 `StatusProcessamento.cs`

Enum: Sucesso, Revisar, Erro

#### 5.3.3 `ModoOperacao.cs`

Enum: Seguro, Automatico

---

### 5.4 `src/OrganizadorDocumentos.Core/Constants/` — Constantes

#### 5.4.1 `SiglasDocumento.cs`

Classe estática que mapeia siglas a descrições.
- Constantes: VT, VA, AC, BO, CO, SP, DE, SE, SB, OS
- `Descricoes` (Dictionary) — Mapeamento sigla → descrição completa
- `ValidaSigla(string)` → bool — Verifica se sigla é válida
- `ObterDescricao(string)` → string — Retorna descrição da sigla

---

### 5.5 `src/OrganizadorDocumentos.Core/Services/Interfaces/` — Interfaces dos Serviços

#### 5.5.1 `IProcessamentoService.cs`

Interface principal de processamento de documentos.
- `ProcessarDocumentoAsync(string caminhoPdf)` → Task<ResultadoProcessamento> — Processa um PDF
- `ProcessarLoteAsync(List<string> arquivos, IProgress<ProgressoProcessamento>?)` → Task<List<ResultadoProcessamento>> — Processa lote de PDFs
- `DocumentoProcessado` (event) — Disparado ao final de cada processamento

Classe auxiliar `ProgressoProcessamento`: Total, Processados, ArquivoAtual, Percentual

#### 5.5.2 `INormalizacaoService.cs`

Interface de normalização de texto.
- `NormalizarNome(string)` → string — Remove acentos, partículas, caracteres especiais
- `CalcularSimilaridade(string, string)` → double — Score fuzzy 0-1
- `SãoEquivalentes(string, string, double limiar = 0.80)` → bool — Compara com limiar
- `NomeMesPorExtenso(int)` → string — Converte 1-12 para "Janeiro"-"Dezembro"

#### 5.5.3 `IMapeamentoService.cs`

Interface de mapeamento da estrutura de pastas.
- `MapearEstrutura(string pastaRaiz)` → EstruturaPasta — Varre o disco
- `AtualizarMapeamento()` → void — Re-escaneia
- `ObterMapeamento()` → EstruturaPasta — Retorna cache em memória
- `BuscarColaboradoresCompativeis(string)` → List<Colaborador> — Fuzzy match
- `MapeamentoAtualizado` (event) — Disparado ao atualizar

Classe auxiliar `MapeamentoEventArgs`: Contém Estrutura (EstruturaPasta)

#### 5.5.4 `ILogService.cs`

Interface de logging.
- `Informacao(string mensagem)`
- `Aviso(string mensagem)`
- `Erro(string mensagem, Exception? excecao = null)`
- `Debug(string mensagem)`

#### 5.5.5 `IFileService.cs`

Interface de operações de arquivo.
- `PastaExiste(string caminho)` → bool
- `ArquivoExiste(string caminho)` → bool
- `BuscarPastaAno(int ano, string caminhoColaborador)` → string
- `BuscarPastaMes(int mes, int ano, string caminhoAno)` → string
- `MoverArquivo(string origem, string destino, bool sobrescrever = false)` → void
- `CriarPasta(string caminho)` → void
- `ListarPdfs(string pasta)` → List<string>
- `NomeArquivoUnico(string caminhoDestino, string nomeBase, string extensao)` → string

#### 5.5.6 `IConfiguracaoService.cs`

Interface de gerenciamento de configurações.
- `CarregarConfiguracao()` → AppConfig
- `SalvarConfiguracao(AppConfig config)` → void
- `ObterConfiguracao()` → AppConfig
- `AtualizarConfiguracao(Action<AppConfig> atualizador)` → void

Classe `AppConfig`: Data class com propriedades: PastaRaiz, PastaColaboradores, PastaRevisar, PastaEntrada, ApiKey, ApiModel, ModoOperacao, LimiarFuzzy, IntervaloVarreduraMinutos, MascaraMes

#### 5.5.7 `IApiService.cs`

Interface de comunicação com IA.
- `ExtrairDadosAsync(string caminhoPdf)` → Task<DocumentoFinanceiro>

---

### 5.6 `src/OrganizadorDocumentos.Core/Services/` — Implementações dos Serviços

#### 5.6.1 `ProcessamentoService.cs` — Orquestrador Principal

Implementa IProcessamentoService. É o "gerente" central do sistema.

**Construtor:** Injeta IApiService, IFileService, IMapeamentoService, INormalizacaoService, IConfiguracaoService, ILogService

**Função principal — ProcessarDocumentoAsync:**
1. Chama ApiService para extrair dados do PDF
2. Verifica limiar de confiança (≥ 70%) → se falhar, vai para REVISAR
3. Verifica competência → se não identificada, REVISAR
4. Verifica sigla → se não identificada, REVISAR
5. Busca colaborador no mapeamento (fuzzy match):
   - 0 matches → Cria novo colaborador automaticamente
   - 1 match → Procura/cria pasta Ano/Mês, move o arquivo
   - Múltiplos matches → REVISAR
   - Arquivo já existe no destino → REVISAR
6. Dispara evento DocumentoProcessado

**Função — ProcessarLoteAsync:** Processa lista de PDFs com relatório de progresso (IProgress<ProgressoProcessamento>)

**Função — CriarColaboradorENovoAsync:** Cria pasta do colaborador, ano e mês, depois move o arquivo

**Função — GerarNomeArquivo:** Gera nome no formato:
- VT_JoaoSilva_05-2025.pdf (padrão)
- OS_JoaoSilva_OS-123.pdf (para documentos por OS)

#### 5.6.2 `NormalizacaoService.cs` — "Tradutor" de Nomes

Implementa INormalizacaoService. Resolve o problema "João da Silva" ≠ "JOAO_DA_SILVA".

**Funções principais:**
- NormalizarNome: Lowercase, remove acentos (tabela de 30+ caracteres), converte underscores para espaços, remove caracteres não-alfanuméricos, colapsa espaços, remove partículas portuguesas (da, de, do, das, dos, e)
- CalcularSimilaridade: Usa FuzzySharp com 4 algoritmos (Ratio, PartialRatio, TokenSortRatio, TokenSetRatio), retorna o melhor resultado / 100
- SãoEquivalentes: Verifica se similaridade ≥ limiar (padrão 0.85)
- NomeMesPorExtenso: Mapeia 1-12 para nomes em português

#### 5.6.3 `MapeamentoService.cs` — "Cartógrafo"

Implementa IMapeamentoService. Varre a pasta COLABORADORES e monta um mapa em memória.

**Função — MapearEstrutura:**
1. Varre COLABORADORES → identifica pastas de colaboradores
2. Para cada colaborador, varre subpastas → identifica anos (regex ^\d{4})
3. Para cada ano, varre subpastas → identifica meses (número ou nome por extenso)
4. Monta EstruturaPasta completa e armazena em cache

**Função — BuscarColaboradoresCompativeis:** Fuzzy match dos nomes normalizados usando SãoEquivalentes

**Função — ExtrairNumeroMes:** Extrai número do mês de qualquer formato de nome de pasta

#### 5.6.4 `LogService.cs` — "Diário de Bordo"

Implementa ILogService. Usa Serilog com:
- Rolling interval por dia
- Template: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}
- Níveis: Debug, Information, Warning, Error

#### 5.6.5 `ConfiguracaoService.cs` — "Gestor de Configurações"

Implementa IConfiguracaoService. CRUD de AppConfig em JSON.
- CarregarConfiguracao: Leitura com cache em memória; se não existir, cria com defaults
- SalvarConfiguracao: Serializa com Newtonsoft.Json (formato indentado)
- ObterConfiguracao: Retorna cache (carrega se necessário)
- AtualizarConfiguracao: Padrão read-modify-save via delegate Action<AppConfig>

#### 5.6.6 `ApiService.cs` — "Porta-voz com a IA"

Implementa IApiService. Comunicação com OpenRouter API.

**Função — ExtrairDadosAsync:**
1. Tenta extrair texto do PDF com iText7 (PdfReader + GetTextFromPage)
2. Verifica se o texto contém padrão de competência (datas, nomes de meses, anos)
3. **Se texto suficiente** → Envia texto para IA (caminho rápido)
4. **Se texto insuficiente** (digitalizado/escrita à mão) → Envia imagens para IA:
   a. Magick.NET converte páginas do PDF em imagens JPEG (200 DPI, até 8 páginas)
   b. Imagens são enviadas como base64 no formato multimodal (OpenAI-compatible)
   c. Gemini (modelo com visão) lê o handwriting e extrai os dados
5. Parseia resposta JSON da IA extraindo: colaborador, sigla, competência, data, OS, confiança

**Prompt do sistema (SystemPrompt):** Instruções detalhadas para extração de documentos brasileiros, com regras especiais para distinguir "Emitente" em RECIBOS vs VALES/BOLETOS, e regras para identificar competência mesmo em texto distorcido.

**Configuração:** Timeout 120s, headers: Authorization (Bearer), HTTP-Referer, X-Title

**Quando envia texto vs imagens:**
| Tipo de PDF | Método | Modelo |
|-------------|--------|--------|
| Texto selecionável (PDF digital) | Texto via iText7 | Gemini Flash |
| Digitalizado / Escrita à mão | Imagens base64 (multimodal) | Gemini Flash |

**Fluxo completo:**
```
PDF → iText7 → texto?
     │
     ├── Texto com datas? → Envia texto para IA → ✅
     │
     └── Texto sem datas? (digitalizado/à mão)
          │
          ▼
         Magick.NET converte PDF → Imagens JPEG (200 DPI, até 8 páginas)
          │
          ▼
         Envia imagens como base64 para IA (multimodal)
          │
          ├── IA leu os dados? → Extrai JSON → ✅
          │
          └── IA não leu? → Vai para REVISAR
```

**Formato da requisição multimodal (OpenAI-compatible):**
```json
{
  "messages": [{
    "role": "user",
    "content": [
      {"type": "text", "text": "Analise este documento..."},
      {"type": "image_url", "image_url": {"url": "data:image/jpeg;base64,..."}},
      {"type": "image_url", "image_url": {"url": "data:image/jpeg;base64,..."}}
    ]
  }]
}
```

**OCR — Requisitos:**
- `tessdata/por.traineddata` deve existir na pasta do aplicativo (baixar de https://github.com/tesseract-ocr/tessdata)
- Ghostscript deve estar instalado (para Magick.NET renderizar PDFs)
- Pacotes NuGet: Tesseract 5.2.0, Magick.NET-Q16-AnyCPU 13.1.0

**Nota sobre handwriting (escrita à mão):**
- Tesseract **NÃO** é eficaz para reconhecer caligrafia
- Para PDFs com escrita à mão, o sistema envia imagens diretamente para IA (Gemini)
- A IA com visão é muito superior em ler handwriting comparado ao OCR tradicional

PDF → iText7 → texto? → SIM → envia para IA
                   → NÃO → Magick.NET (PDF → imagem)
                               → Tesseract (OCR português)
                                   → texto? → SIM → envia para IA
                                            → NÃO → REVISAR
```

#### 5.6.7 `FileService.cs` — "Operador de Arquivos"

Implementa IFileService. Operações de sistema de arquivo.

**Funções principais:**
- BuscarPastaAno: Busca pasta por nome exato, depois variações (prefix, regex). Cria se não existir.
- BuscarPastaMes: Tenta 6 variações de nomenclatura ("05 - Maio", "05 Maio", "05_Maio", "Maio", "05", "05"). Reconhece pastas existentes por normalização ou prefixo numérico. Cria "MM - NomeExtenso" se não achar.
- MoverArquivo: Move com tratamento de colisão (gera nome único com _1, _2, etc. se não for sobrescrever)
- NomeArquivoUnico: Itera contador até encontrar nome disponível
- ListarPdfs: Lista apenas *.pdf (ordenados) no diretório

---

### 5.7 `src/OrganizadorDocumentos.Core/Class1.cs`

Placeholder vazio — restante de scaffolding. Sem funcionalidade.

### 5.8 `src/OrganizadorDocumentos.Data/` — Camada de Dados (Vazia)

#### 5.8.1 `OrganizadorDocumentos.Data.csproj`

Classe library vazia, sem dependências. Preparada para futuras implementações de acesso a dados.

#### 5.8.2 `Class1.cs`

Placeholder vazio. Sem funcionalidade.

---

## 6. Pasta `src/OrganizadorDocumentos.UI/` — Interface Gráfica (WPF)

### 6.1 `App.xaml`

Função: Definição global da aplicação WPF.

- Registra 4 value converters como recursos estáticos
- Define DataTemplate para cada ViewModel → View (permite que ContentControl renderize automaticamente)
- Define estilos reutilizáveis:
  - MenuButtonStyle — Botões transparentes da barra lateral
  - ActionButtonStyle — Botões azuis
  - SuccessButtonStyle — Botões verdes (herda ActionButtonStyle)
  - WarningButtonStyle — Botões laranja (herda ActionButtonStyle)
  - CardStyle — Borda branca arredondada com sombra
  - HeaderTextStyle, LabelTextStyle, ValueTextStyle — Estilos de texto

### 6.2 `App.xaml.cs`

Função: Entry point da aplicação WPF.

**OnStartup:**
1. Cria ServiceCollection
2. Chama ConfigureServices() — registro de todas as dependências (singleton)
3. Cria diretórios %APPDATA%\OrganizadorDocumentos e \logs
4. Define caminhos: config.json e log_YYYY-MM-DD.txt
5. Registra serviços: ConfiguracaoService, LogService, NormalizacaoService, FileService, MapeamentoService, ApiService, ProcessamentoService
6. Registra ViewModels: Dashboard, Mapeamento, Processamento, Revisao, Configuracao
7. Registra MainViewModel e MainWindow
8. Resolve MainWindow do container e exibe

**OnExit:** Disposing do ServiceProvider.

### 6.3 `MainWindow.xaml`

Função: Janela principal com layout de dois painéis.

- Coluna 1 (220px): Barra lateral escura (#2C3E50) com:
  - Header "Organizador de Documentos"
  - 5 botões de navegação (Dashboard, Mapeamento, Processamento, Revisão, Configurações) com comandos do MainViewModel
  - Status do sistema no rodapé
- Coluna 2 (*): ContentControl (ContentArea) que exibe a view ativa
- Title bound a ViewModel.Titulo, Height=700, Width=1100

### 6.4 `MainWindow.xaml.cs`

Construtor: Recebe MainViewModel via DI. Cria as 5 Views, atribui DataContexts, assina NavegacaoSolicitada. Ao iniciar, navega automaticamente para Dashboard.

OnNavegacaoSolicitada: Troca ContentArea.Content entre as 5 views com base no nome da view recebido.

---

### 6.5 `src/OrganizadorDocumentos.UI/ViewModels/` — ViewModels (Lógica da Interface)

#### 6.5.1 `ViewModelBase.cs`

Classe abstrata base implementando INotifyPropertyChanged.
- SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName) — Define valor e dispara PropertyChanged apenas se diferente
- OnPropertyChanged([CallerMemberName] string? propertyName) — Dispara evento

#### 6.5.2 `RelayCommand.cs`

Implementação do padrão ICommand para MVVM.
- RelayCommand — Aceita Action<object?> e Predicate<object?>?
- RelayCommand<T> — Versão genérica forte
- Ambos wire CanExecuteChanged ao CommandManager.RequerySuggested

#### 6.5.3 `MainViewModel.cs` — "Controlador Central"

VM raiz da janela principal.
- Propriedades: Titulo, StatusSistema
- 5 sub-ViewModels injetados: Dashboard, Mapeamento, Processamento, Revisao, Configuracao
- 5 comandos de navegação (NavegarDashboardCommand, etc.) → disparam evento NavegacaoSolicitada
- NavegarPara(string) → atualiza título e dispara navegação

#### 6.5.4 `DashboardViewModel.cs` — Painel de Estatísticas

Exibe métricas gerais do sistema.
- Injeita: IProcessamentoService, IMapeamentoService
- Propriedades: TotalProcessados, TotalRevisar, TotalErros, UltimaAtualizacao, Processando
- AtualizarEstatisticas() → Atualiza timestamp de última atualização

#### 6.5.5 `MapeamentoViewModel.cs` — Mapeamento de Pastas

VM da tela de mapeamento.
- Injeita: IMapeamentoService, IConfiguracaoService, ILogService
- Propriedades: TotalColaboradores, TotalAnos, TotalPastasMensais, Mapeando, StatusMapeamento
- Colaboradores (ObservableCollection<Colaborador>) — Lista para ListView
- AtualizarEstruturaCommand → Executa MapearEstrutura em background thread via Task.Run, atualiza UI
- CarregarMapeamentoExistente() → Carrega cache ao iniciar

#### 6.5.6 `ProcessamentoViewModel.cs` — Processamento de PDFs

VM da tela de processamento.
- Injeita: IProcessamentoService, IFileService, IConfiguracaoService, ILogService
- Propriedades: Processando, Progresso, ArquivoAtual, TotalArquivos, Processados, Revisar, Erros
- Resultados (ObservableCollection<ResultadoProcessamento>) — Lista de resultados
- ArquivosPendentes (ObservableCollection<string>) — Lista de PDFs na ENTRADA
- ProcessarCommand → Chama ProcessarLoteAsync com progresso via Progress<ProgressoProcessamento>
- AtualizarListaCommand → Lista PDFs da pasta ENTRADA
- Ao iniciar, já atualiza a lista de pendentes

#### 6.5.7 `RevisaoViewModel.cs` — Revisão Manual

VM da tela de revisão. Para documentos que a IA não entendeu.
- Injeita: IFileService, IConfiguracaoService, ILogService, IMapeamentoService, INormalizacaoService
- Lista de PDFs em REVISAR: ArquivosRevisar (ObservableCollection<string>)
- Arquivo selecionado: ArquivoSelecionado → ao mudar, dispara CarregarDadosArquivo (preenche competência com data atual)
- Campos do formulário: Colaborador, Sigla, CompetenciaMes, CompetenciaAno, Data, NumeroOS, StatusMensagem
- Comandos:
  - AtualizarListaCommand → Atualiza lista de PDFs em REVISAR
  - AbrirPastaRevisarCommand → Abre explorer.exe na pasta REVISAR
  - ProcessarManualCommand → Valida campos, busca/cria colaborador, ano/mês, move arquivo
  - LimparFormularioCommand → Reseta campos
- PodeProcessar() → Valida que todos os campos obrigatórios estão preenchidos e sigla é válida
- GerarNomeArquivo → Gera nome no padrão {Sigla}_{Colaborador}_{MM-AAAA}.pdf ou OS_{Colaborador}_OS-{Numero}.pdf

#### 6.5.8 `ConfiguracaoViewModel.cs` — Configurações do Usuário

VM da tela de configurações.
- Injeita: IConfiguracaoService, ILogService
- Propriedades (2-way binding): PastaRaiz, PastaColaboradores, PastaRevisar, PastaEntrada, ApiKey, ApiModel, ModoOperacao, LimiarFuzzy, MascaraMes, StatusSalvamento
- SelecionarPastaCommand → Abre OpenFolderDialog, define PastaRaiz
- SalvarCommand → Chama AtualizarConfiguracao com todos os valores atuais
- CarregarConfiguracao() → Preenche propriedades do config salvo

---

### 6.6 `src/OrganizadorDocumentos.UI/Views/` — Views (Telas WPF)

#### 6.6.1 `ViewsCodeBehind.cs`

Contém code-behind de todas as 5 views como partial classes.
- DashboardView, MapeamentoView, ProcessamentoView, RevisaoView — Construtores vazios (apenas InitializeComponent())
- ConfiguracaoView — Possui lógica de sincronização da API Key:
  - _restaurandoSenha (flag anti-feedback) para evitar loop entre PasswordBox e ViewModel
  - ConfiguracaoView_Loaded → Preenche PasswordBox do VM
  - ApiKeyPasswordBox_PasswordChanged → Sincroniza de volta para VM

#### 6.6.2 `DashboardView.xaml`

Tela de dashboard com:
- 3 cards de métricas (Processados verde, Revisar laranja, Erros vermelho)
- Card "Informações do Sistema" — Data/hora da última atualização
- Card "Regra Principal" — "NÃO CRIAR SE JÁ EXISTIR"

#### 6.6.3 `MapeamentoView.xaml`

Tela de mapeamento com:
- Botão "Atualizar estrutura" (desabilita durante mapeamento)
- Progress bar indeterminate durante operação
- 3 cards de contagem (Colaboradores azul, Anos roxo, Pastas Mensais turquesa)
- ListView de colaboradores (Pasta, Nome Normalizado, Anos)

#### 6.6.4 `ProcessamentoView.xaml`

Tela de processamento com:
- Botões "Processar Agora" (verde) e "Atualizar Lista" (azul)
- Progress bar de processamento
- 4 cards de contagem (Pendentes azul, Processados verde, Revisar laranja, Erros vermelho)
- ListBox de PDFs pendentes (com FileNameConverter)
- ListView de resultados (Arquivo, Status, Destino, Mensagem)

#### 6.6.5 `RevisaoView.xaml`

Tela de revisão manual com:
- Botões "Atualizar Lista" e "Abrir Pasta REVISAR"
- Card mostrando caminho da pasta REVISAR
- ListView de PDFs com SelectedItem binding
- Formulário (visível apenas se arquivo selecionado):
  - TextBox Colaborador
  - ComboBox Sigla (10 siglas fixas: VT, VA, AC, BO, CO, SP, DE, SE, SB, OS)
  - ComboBox Mês (1-12) e Ano (2024-2028)
  - TextBox Data e NumeroOS
  - Botões Processar (verde) e Limpar (laranja)
  - Label de status
- Ação informativa quando nenhum arquivo está selecionado

#### 6.6.6 `ConfiguracaoView.xaml`

Tela de configurações com 3 seções:
1. Pastas do Sistema: Pasta Raiz (com botão "Procurar..."), Colaboradores, Revisar, Entrada
2. Configurações da API (OpenRouter): API Key (PasswordBox), Modelo
3. Comportamento: Modo Operação (seguro/automatico), Limiar Fuzzy (slider 0.5-1.0), Máscara Mês
- Botão "Salvar Configurações" (verde) + label de status

---

### 6.7 `src/OrganizadorDocumentos.UI/Converters/` — Conversores WPF

#### 6.7.1 `Converters.cs`

4 ValueConverters para bindings WPF:
- InverseBooleanConverter — Inverte valor booleano (true→false, false→true)
- BooleanToVisibilityConverter — Converte bool → Visibility (Visible/Collapsed). Com parâmetro "Inverse" inverte.
- FileNameConverter — Extrai apenas o nome de arquivo de um caminho completo (Path.GetFileName)
- CountToVisibilityConverter — Mostra Visible se count > 0, Collapsed caso contrário

---

## 7. Pasta `tests/` — Testes Automatizados

### 7.1 Estrutura

tests/OrganizadorDocumentos.Tests/
├── OrganizadorDocumentos.Tests.csproj
├── FileServiceTests.cs
└── NormalizacaoServiceTests.cs

### 7.2 `OrganizadorDocumentos.Tests.csproj`

Tipo: Test Project (xUnit)
Target: net10.0
Pacotes: xUnit 2.9.3, xunit.runner.visualstudio 3.1.4, coverlet.collector 6.0.4, Microsoft.NET.Test.Sdk 17.14.1
Referências: Core e Data projects

### 7.3 `FileServiceTests.cs`

6 testes para FileService:

| Teste | Cenário | Resultado esperado |
|-------|---------|-------------------|
| BuscarPastaAno_PastaExistente_UsaExistente | Pasta 2026 existe | Retorna caminho existente |
| BuscarPastaAno_PastaNaoExistente_CriaNova | Pasta 2025 não existe | Cria e retorna nova pasta |
| BuscarPastaMes_PastaExistente_UsaExistente | "08 - Agosto" existe | Retorna pasta existente |
| BuscarPastaMes_VariacaoNomenclatura_ReconheceEquivalente | "Agosto" (sem número) | Reconhece como mês 8 |
| BuscarPastaMes_PastaNaoExistente_CriaNova | Março não existe | Cria pasta com "Março" |
| ListarPdfs_RetornaApenasPdfs | 2 .pdf + 1 .txt | Retorna apenas 2 PDFs |

Setup: Cria diretório temporário único por classe (Guid.NewGuid()), LogService em test.log. Nota: Dispose() não implementa IDisposable corretamente para xUnit.

### 7.4 `NormalizacaoServiceTests.cs`

11 testes (cases+theories) para NormalizacaoService:

| Teste | Tipo | Casos | Verificação |
|-------|------|-------|------------|
| NormalizarNome_RemoveAcentosECaixaAlta | Theory | 6 cases | "João da Silva" → "joao silva" |
| CalcularSimilaridade_NomesEquivalentes_RetornaAltaSimilaridade | Theory | 3 cases | Similaridade ≥ 0.75 |
| SãoEquivalentes_NomesIguais_RetornaTrue | Fact | 1 | "João da Silva" ≈ "JOAO_DA_SILVA" |
| SãoEquivalentes_NomesDiferentes_RetornaFalse | Fact | 1 | "João" vs "Maria" → false |
| NomeMesPorExtenso_RetornaNomeCorreto | Theory | 4 cases | 1→Janeiro, 2→Fevereiro, 8→Agosto, 12→Dezembro |

Nota: SãoEquivalentes_NomesIguais_RetornaTrue testa CalcularSimilaridade diretamente, não o método SãoEquivalentes.

---

## 8. Pasta `bin/` — Artefatos de Build

Contém todos os assemblies compilados e dependências:

**Executável principal:** OrganizadorDocumentos.UI.exe (+ .dll, .pdb, .deps.json, .runtimeconfig.json)

**Assemblies do projeto:**
- OrganizadorDocumentos.Core.dll/pdb
- OrganizadorDocumentos.Data.dll/pdb

**Dependências NuGet:**
- iText7 (itext.kernel, itext.layout, itext.pdfua, itext.pdfa, itext.sign, itext.svg, itext.styledxmlparser, itext.io, itext.commons, itext.bouncy-castle-connector, itext.forms, itext.barcodes)
- FuzzySharp.dll — Comparação fuzzy de strings
- Serilog.dll + Serilog.Sinks.File.dll — Logging
- Newtonsoft.Json.dll — JSON
- Microsoft.Extensions. (DI, Logging, Options, Primitives, DependencyInjection, DependencyModel, PlatformAbstractions, Abstractions)

---

## 8. Fluxo de Dados do Sistema

```
1. Usuário coloca PDF na pasta ENTRADA
2. Clica "Processar" na tela Processamento
3. ProcessamentoViewModel → ProcessarLoteAsync()
4. ProcessamentoService.ProcessarDocumentoAsync() para cada PDF:
   a. ApiService.ExtrairDadosAsync():
      5. iText7 extrai texto do PDF (todas as páginas)
      6. Envia texto + prompt para OpenRouter API
      7. Parseia JSON da resposta → DocumentoFinanceiro
   b. Validações (confiança ≥ 70%, colaborador, competência, sigla)
   c. MapeamentoService.BuscarColaboradoresCompativeis() (fuzzy match)
   d. FileService cria/move arquivo para pasta destino
   e. Se falhar → move para REVISAR
5. Resultados exibidos na tela Processamento
6. Arquivos em REVISAR acessíveis na tela Revisão para processamento manual
```

**Armazenamento de configuração:** %APPDATA%\OrganizadorDocumentos\config.json
**Logs:** %APPDATA%\OrganizadorDocumentos\logs\log_YYYY-MM-DD.txt