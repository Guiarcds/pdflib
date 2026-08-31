# Plano: Organizador Inteligente de Documentos Financeiros

## Visão Geral

Aplicativo desktop Windows (C# + WPF) para organizar automaticamente PDFs financeiros usando IA (OpenRouter AI) exclusivamente para interpretação de documentos. O aplicativo controla todas as operações de sistema de arquivos.

---

## Decisões Técnicas Confirmadas

| Aspecto | Decisão |
|---------|---------|
| Stack | C# + WPF (.NET 6+) |
| IA | OpenRouter API (interpretação de PDFs) |
| PDF | Texto extraído via API do provedor |
| Arquitetura | MVVM em camadas |
| Configuração | Arquivo JSON local (AppData) |
| Processamento | Varredura em lote (batch) |
| Logs | Arquivo de log local |
| Rede | Suporte a UNC paths (\\servidor\pasta) |
| Idioma UI | Português |
| Match de nomes | Fuzzy matching (Levenshtein + normalização) |
| Conflitos | Enviar para REVISAR |

---

## Arquitetura do Sistema

### Estrutura de Camadas

```
OrganizadorDocumentos/
├── OrganizadorDocumentos.UI/          (WPF - Views)
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   ├── DashboardView.xaml
│   │   ├── MapeamentoView.xaml
│   │   ├── ProcessamentoView.xaml
│   │   ├── RevisaoView.xaml
│   │   └── ConfiguracaoView.xaml
│   └── ViewModels/
│       ├── MainViewModel.cs
│       ├── DashboardViewModel.cs
│       ├── MapeamentoViewModel.cs
│       ├── ProcessamentoViewModel.cs
│       ├── RevisaoViewModel.cs
│       └── ConfiguracaoViewModel.cs
│
├── OrganizadorDocumentos.Core/        (Lógica de negócio)
│   ├── Models/
│   │   ├── Colaborador.cs
│   │   ├── DocumentoFinanceiro.cs
│   │   ├── Competencia.cs
│   │   ├── ClassificacaoDocumento.cs
│   │   ├── ResultadoProcessamento.cs
│   │   └── EstruturaPasta.cs
│   ├── Enums/
│   │   ├── TipoDocumento.cs          (VT, VA, AC, BO, CO, SP, DE, SE, SB, OS)
│   │   ├── ModoOperacao.cs            (Seguro, Automatico)
│   │   └── StatusProcessamento.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IApiService.cs
│   │   │   ├── IFileService.cs
│   │   │   ├── IMapeamentoService.cs
│   │   │   ├── IProcessamentoService.cs
│   │   │   ├── INormalizacaoService.cs
│   │   │   ├── IConfiguracaoService.cs
│   │   │   └── ILogService.cs
│   │   ├── ApiService.cs              (OpenRouter AI)
│   │   ├── FileService.cs             (Sistema de arquivos)
│   │   ├── MapeamentoService.cs       (Varredura de estrutura)
│   │   ├── ProcessamentoService.cs    (Pipeline de processamento)
│   │   ├── NormalizacaoService.cs     (Fuzzy matching de nomes)
│   │   ├── ConfiguracaoService.cs     (Gestão de config)
│   │   └── LogService.cs              (Logs em arquivo)
│   └── Constants/
│       └── SiglasDocumento.cs
│
├── OrganizadorDocumentos.Data/        (Persistência)
│   ├── Config/
│   │   └── AppConfig.cs
│   └── Repositories/
│       └── ConfigRepository.cs
│
└── OrganizadorDocumentos.Tests/       (Testes unitários)
    └── Services/
```

---

## Componentes Principais

### 1. Sistema de Configuração

**Arquivo**: `AppConfig.cs` + `ConfigRepository.cs`

```json
{
  "pasta_raiz": "\\\\servidor\\financeiro",
  "pasta_colaboradores": "COLABORADORES",
  "pasta_revisar": "REVISAR",
  "pasta_entrada": "ENTRADA",
  "api_key": "sk-or-...",
  "api_model": "google/gemini-2.0-flash-001",
  "modo_operacao": "seguro",
  "limiar_fuzzy": 0.80,
  "intervalo_varredura_minutos": 30,
  "mascara_mes": "00 - Mmmm"
}
```

**Responsabilidades**:
- Carregar/salvar configurações em `%AppData%\OrganizadorDocumentos\config.json`
- Validar configurações obrigatórias
- Fornecer valores padrão

---

### 2. Serviço de Mapeamento de Estrutura

**Arquivo**: `MapeamentoService.cs`

**Objetivo**: Construir cadastro da estrutura existente sem modificá-la.

**Fluxo**:
```
Pasta Raiz
    └── COLABORADORES/
        ├── JOAO_DA_SILVA/     → Colaborador (nome normalizado: joao da silva)
        │   ├── 2024/          → Ano
        │   ├── 2025/          → Ano
        │   └── 2026/
        │       ├── 01 - Janeiro/   → Mês (1)
        │       └── 08 - Agosto/    → Mês (8)
        └── MARIA_DE_SAUZA/
```

**Modelo de Dados**:
```csharp
class EstruturaPasta
{
    List<Colaborador> Colaboradores { get; }
    int TotalAnos { get; }
    int TotalPastasMensais { get; }
}

class Colaborador
{
    string NomePasta { get; }        // "JOAO_DA_SILVA"
    string NomeNormalizado { get; }  // "joao da silva"
    string CaminhoCompleto { get; }
    List<PastaAno> Anos { get; }
}

class PastaAno
{
    int Ano { get; }                 // 2026
    string NomePasta { get; }        // "2026"
    string CaminhoCompleto { get; }
    List<PastaMes> Meses { get; }
}

class PastaMes
{
    int NumeroMes { get; }           // 8
    string NomePasta { get; }        // "08 - Agosto"
    string NomeNormalizado { get; }  // "08 agosto"
    string CaminhoCompleto { get; }
}
```

**Métodos**:
- `EstruturaPasta MapearEstrutura(string pastaRaiz)` — varredura completa
- `void AtualizarMapeamento()` — refresh do mapeamento
- `Colaborador BuscarColaborador(string nomeNormalizado)` — busca com fuzzy

---

### 3. Serviço de Normalização e Fuzzy Matching

**Arquivo**: `NormalizacaoService.cs`

**Objetivo**: Comparar nomes ignorando acentos, case, espaços e pequenas variações.

**Algoritmo**:
1. **Normalização**:
   - Remover acentos (ã→a, é→e, ç→c)
   - Converter para minúsculas
   - Remover caracteres especiais (exceto letras, números, espaços)
   - Normalizar espaços (múltiplos → único, trim)
   - Substituir espaços por underscore para comparação com pastas

2. **Fuzzy Matching** (Levenshtein Distance):
   - Calcular distância entre nomes normalizados
   - Converter para similaridade (0.0 a 1.0)
   - Limiar configurável (padrão: 0.80)

3. **Regras de Match**:
   - Similaridade >= limiar → MATCH
   - Similaridade < limiar → NÃO MATCH
   - Múltiplos matches acima do limiar → AMBIGUO (enviar REVISAR)

**Exemplo**:
```
PDF: "João da Silva" → normalizado: "joao da silva"
Pasta: "JOAO_DA_SILVA" → normalizado: "joao da silva"
Similaridade: 1.0 → MATCH ✓
```

---

### 4. Serviço de API (OpenRouter AI)

**Arquivo**: `ApiService.cs`

**Objetivo**: Enviar PDF para interpretação e receber dados estruturados.

**Endpoint**: `https://openrouter.ai/api/v1/chat/completions`

**Prompt do Sistema**:
```
Você é um especialista em documentos financeiros brasileiros.
Analise o PDF fornecido e extraia APENAS as informações solicitadas.
NÃO invente informações. Se não encontrar, retorne null.

Retorne um JSON válido com:
{
  "colaborador": "nome completo ou null",
  "tipo_documento": "descrição do tipo ou null",
  "sigla": "VT|VA|AC|BO|CO|SP|DE|SE|SB|OS|null",
  "competencia": {
    "mes": 1-12 ou null,
    "ano": YYYY ou null
  },
  "data": "DD/MM/YYYY ou null",
  "numero_os": "número ou null",
  "confianca": 0.0-1.0
}

Siglas: VT=Vale Transporte, VA=Vale Alimentação, AC=Ajuda de Custo,
BO=Bonificação, CO=Comissão, SP=Serviço Prestado, DE=Diária,
SE=Salário Extra, SB=Salário Base, OS=Vale por OS
```

**Métodos**:
- `Task<ResultadoExtracao> ExtrairDadosAsync(string caminhoPdf)` — envia PDF e parseia resposta
- `string LerConteudoPdf(string caminhoPdf)` — lê texto do PDF (via API ou biblioteca local)

**Tratamento de Erros**:
- Timeout de requisição
- Rate limiting (429)
- Resposta malformada
- API key inválida

---

### 5. Serviço de Processamento (Pipeline)

**Arquivo**: `ProcessamentoService.cs`

**Objetivo**: Orquestrar o fluxo completo de processamento de um documento.

**Pipeline**:
```
1. Receber PDF da pasta de entrada
2. Extrair dados via IA
3. Validar dados extraídos
4. Buscar colaborador (fuzzy match)
   ├── Encontrou 1 → continuar
   ├── Encontrou 0 → REVISAR (novo colaborador?)
   └── Encontrou N>1 → REVISAR (ambiguidade)
5. Determinar ano da competência
6. Buscar/criar pasta do ano
7. Determinar mês da competência
8. Buscar/criar pasta do mês
9. Verificar conflito de nome
   ├── Existe → REVISAR
   └── Não existe → continuar
10. Renomear arquivo
11. Mover para destino
12. Registrar log
```

**Modelo de Resultado**:
```csharp
class ResultadoProcessamento
{
    string ArquivoOrigem { get; }
    string CaminhoDestino { get; }
    StatusProcessamento Status { get; }  // Sucesso, Revisar, Erro
    string Mensagem { get; }
    DocumentoFinanceiro DadosExtraidos { get; }
}
```

---

### 6. Serviço de Arquivos

**Arquivo**: `FileService.cs`

**Objetivo**: Centralizar TODAS as operações de sistema de arquivos.

**Regra Crítica**: NUNCA criar pasta se já existir equivalente.

**Métodos**:
- `bool PastaExiste(string caminho)` — verifica existência
- `string BuscarPastaAno(int ano, string caminhoColaborador)` — busca ou cria
- `string BuscarPastaMes(int mes, int ano, string caminhoAno)` — busca ou cria
- `bool ArquivoExiste(string caminho)` — verifica conflito
- `void MoverArquivo(string origem, string destino)` — move arquivo
- `void CriarPasta(string caminho)` — cria pasta (após verificação)
- `List<string> ListarPdfs(string pasta)` — lista PDFs para processar

**Lógica de Busca de Pasta de Ano**:
```csharp
string BuscarPastaAno(int ano, string caminhoColaborador)
{
    var diretorios = Directory.GetDirectories(caminhoColaborador);
    
    // Busca exata primeiro
    foreach (var dir in diretorios)
    {
        var nome = Path.GetFileName(dir);
        if (nome == ano.ToString())
            return dir; // Usa existente
    }
    
    // Busca variações (2026_2, 2026-NOVO, etc.)
    foreach (var dir in diretorios)
    {
        var nome = Path.GetFileName(dir);
        if (nome.StartsWith(ano.ToString()) || 
            Regex.IsMatch(nome, $"^{ano}[_\\s-]"))
            return dir; // Usa existente
    }
    
    // Não encontrou → cria
    var novoCaminho = Path.Combine(caminhoColaborador, ano.ToString());
    Directory.CreateDirectory(novoCaminho);
    return novoCaminho;
}
```

**Lógica de Busca de Pasta de Mês**:
```csharp
string BuscarPastaMes(int mes, int ano, string caminhoAno)
{
    var diretorios = Directory.GetDirectories(caminhoAno);
    var nomesPossiveis = new[] {
        $"{mes:D2} - {NomeMes(mes)}",  // "08 - Agosto"
        $"{mes:D2} {NomeMes(mes)}",     // "08 Agosto"
        $"{mes:D2}_{NomeMes(mes)}",     // "08_Agosto"
        NomeMes(mes),                    // "Agosto"
        mes.ToString("D2"),              // "08"
        mes.ToString()                   // "8"
    };
    
    foreach (var dir in diretorios)
    {
        var nome = Path.GetFileName(dir);
        var nomeNorm = Normalizar(nome);
        
        foreach (var possivel in nomesPossiveis)
        {
            if (nomeNorm == Normalizar(possivel))
                return dir; // Usa existente
        }
    }
    
    // Não encontrou → cria com padrão configurado
    var padrao = _config.MascaraMes; // "00 - Mmmm"
    var novoNome = padrao.Replace("00", mes.ToString("D2"))
                          .Replace("Mmmm", NomeMes(mes));
    var novoCaminho = Path.Combine(caminhoAno, novoNome);
    Directory.CreateDirectory(novoCaminho);
    return novoCaminho;
}
```

---

### 7. Interface do Usuário (WPF)

**Telas**:

#### Dashboard (Tela Principal)
- Status do sistema (ativo/processando/parado)
- Estatísticas: documentos processados, em revisar, erros
- Botões: Iniciar Processamento, Atualizar Estrutura, Configurações
- Log em tempo real

#### Mapeamento
- Botão "Atualizar Estrutura"
- Exibição: Colaboradores encontrados, Anos, Pastas mensais
- Árvore visual da estrutura (TreeView)

#### Processamento
- Lista de arquivos na pasta de entrada
- Botão "Processar Agora"
- Barra de progresso
- Resultados: Sucesso / Revisar / Erro

#### Revisão Manual
- Lista de documentos pendentes
- Dados extraídos pela IA (para conferência)
- Ações: Aprovar (mover manualmente), Ignorar, Criar Colaborador

#### Configurações
- Pasta raiz (com botão procurar)
- Pasta de entrada
- Modo operação (Seguro/Automático)
- API Key
- Modelo de IA
- Limiar fuzzy
- Máscara de nome de mês

---

## Fluxo de Execução

### Inicialização do Aplicativo
```
1. Carregar configuração (config.json)
2. Validar configuração (pasta raiz existe?)
3. Executar mapeamento inicial
4. Exibir dashboard com estatísticas
```

### Processamento em Lote
```
1. Listar PDFs na pasta de entrada
2. Para cada PDF:
   a. Extrair dados via IA
   b. Buscar colaborador (fuzzy)
   c. Determinar destino (ano/mês)
   d. Verificar conflitos
   e. Mover e renomear OU enviar REVISAR
3. Atualizar estatísticas
4. Registrar logs
```

### Atualização de Estrutura
```
1. Limpar mapeamento atual
2. Re-escanear pasta raiz
3. Reconstruir índice de colaboradores/anos/meses
4. Exibir resumo ao usuário
```

---

## Tratamento de Erros e Casos Especiais

| Situação | Ação |
|----------|------|
| Colaborador não encontrado | REVISAR + perguntar se deseja criar (modo seguro) |
| Múltiplos colaboradores (fuzzy) | REVISAR (ambiguidade) |
| Competência não identificada | REVISAR |
| Tipo não identificado | REVISAR |
| Arquivo destino já existe | REVISAR |
| Erro de API (timeout, rate limit) | Retry com backoff exponencial |
| PDF ilegível | REVISAR |
| Pasta rede indisponível | Notificar + aguardar reconexão |
| API key inválida | Notificar + parar processamento |

---

## Estrutura de Pastas do Aplicativo

```
%AppData%\OrganizadorDocumentos\
├── config.json           # Configurações do usuário
├── logs\
│   └── log_2026-08-27.txt  # Logs diários
└── cache\
    └── mapeamento.json   # Cache do mapeamento (opcional)
```

---

## Dependências (NuGet)

| Pacote | Finalidade |
|--------|------------|
| `Microsoft.Extensions.DependencyInjection` | Injeção de dependência |
| `Microsoft.Extensions.Configuration` | Configuração |
| `Newtonsoft.Json` | Serialização JSON |
| `FuzzySharp` | Fuzzy matching (Levenshtein) |
| `Serilog` | Logging |
| `Serilog.Sinks.File` | Logs em arquivo |
| `itext7` | Leitura de PDF (fallback) |

---

## Plano de Implementação

### Fase 1: Estrutura Base
1. Criar solução Visual Studio com projetos (UI, Core, Data, Tests)
2. Configurar injeção de dependência
3. Implementar sistema de configuração (JSON)
4. Implementar serviço de logs

### Fase 2: Mapeamento de Estrutura
5. Implementar `NormalizacaoService`
6. Implementar `MapeamentoService`
7. Criar modelos de dados (Colaborador, PastaAno, PastaMes)
8. Testar com estrutura de exemplo

### Fase 3: Integração com IA
9. Implementar `ApiService` (OpenRouter)
10. Criar prompt de extração
11. Implementar parser de resposta JSON
12. Testar com PDFs reais

### Fase 4: Processamento
13. Implementar `FileService`
14. Implementar `ProcessamentoService` (pipeline completo)
15. Implementar lógica "NÃO CRIAR SE JÁ EXISTIR"
16. Testar fluxo completo

### Fase 5: Interface
17. Criar MainWindow (Dashboard)
18. Criar MapeamentoView
19. Criar ProcessamentoView
20. Criar RevisaoView
21. Criar ConfiguracaoView
22. Implementar ViewModels (MVVM)

### Fase 6: Testes e Refinamento
23. Testes unitários (serviços core)
24. Testes de integração
25. Testes com dados reais
26. Ajustes de UX

---

## Validação

### Testes Unitários
- Normalização de nomes (acentos, case, espaços)
- Fuzzy matching (matches corretos, ambiguidades)
- Busca de pastas (existente vs criar nova)
- Geração de nomes de arquivos
- Parser de resposta da IA

### Testes de Integração
- Mapeamento de estrutura real
- Processamento completo de PDF
- Tratamento de erros de API
- Movimentação de arquivos

### Cenários de Teste
1. PDF com colaborador existente → mover corretamente
2. PDF com colaborador novo (modo seguro) → REVISAR
3. PDF com colaborador novo (modo automático) → criar pasta
4. PDF com competência passada → usar ano/mês correto
5. PDF com nome ambíguo → REVISAR
6. Arquivo destino existente → REVISAR
7. Pasta de ano com número (2026_2) → reconhecer como 2026
8. Pasta de mês com variação (Agosto) → reconhecer como 08

---

## Riscos e Mitigações

| Risco | Mitigação |
|-------|-----------|
| API indisponível | Retry com backoff, fila de reprocessamento |
| PDF escaneado (imagem) | Notificar limitação, enviar REVISAR |
| Nome muito curto (ambiguidade) | Limiar fuzzy mais restrito |
| Pasta rede lenta | Timeout configurável, operações assíncronas |
| Rate limiting | Respeitar headers, fila de processamento |

---

## Fora de Escopo (v1)

- Edição de documentos
- Versionamento de arquivos
- Múltiplos idiomas na UI
- Suporte a outros formatos (DOCX, imagens)
- Sincronização em tempo real (FileSystemWatcher)
- Dashboard web
- Relatórios avançados
