# Plano de Implementação: Unificação de Pastas de Colaboradores

## Contexto

O sistema atual organiza documentos em uma estrutura de pastas hierárquica:

```
COLABORADORES/
├── JOAO_SILVA/
│   ├── 2025/
│   │   └── 01 - Janeiro/
│   │       └── VT_JOAO_SILVA_01-2025.pdf
│   └── 2026/
│       └── 02 - Fevereiro/
│           └── VA_JOAO_SILVA_02-2026.pdf
├── JOAO_DA_SILVA/  (mesmo colaborador, pasta com nome diferente)
│   └── 2025/
│       └── 03 - Marco/
│           └── AC_JOAO_DA_SILVA_03-2025.pdf
```

Quando um colaborador é identificado de forma inconsistente (ex: `JOAO_SILVA` vs `JOAO_DA_SILVA`), o conteúdo fica espalhado em duas pastas separadas. Esta funcionalidade permite unificar essas pastas, movendo todo o conteúdo de uma pasta para outra e excluindo a pasta de origem.

## Requisitos

1. **Interface:** Botão "[ Unificar ]" na view de mapeamento, que abre um dialog para selecionar os dois colaboradores
2. **Operação:** Mover todo o conteúdo do colaborador X (origem) para o colaborador Y (destino), preservando a estrutura de anos/meses
3. **Conflitos:** Usar `NomeArquivoUnico` quando houver nomes duplicados
4. **Exclusão:** A pasta do colaborador X é excluída após a conclusão
5. **Mapeamento:** Após a unificação, o mapeamento é atualizado automaticamente

## Arquitetura

### 1. Nova função no `MapeamentoService`

Arquivo: `src/OrganizadorDocumentos.Core/Services/MapeamentoService.cs`

Adicionar método `UnificarColaboradores`:

```csharp
public UnificarResultado UnificarColaboradores(string nomeOrigem, string nomeDestino)
{
    // 1. Validar que ambos existem no mapeamento
    // 2. Obter caminhos completos
    // 3. Chamar FileService.MoverPastaCompleta
    // 4. Atualizar mapeamento
    // 5. Retornar resultado com estatísticas
}
```

### 2. Nova função no `FileService`

Arquivo: `src/OrganizadorDocumentos.Core/Services/FileService.cs`

Adicionar método `MoverPastaCompleta` para mover recursivamente:

```csharp
public void MoverPastaCompleta(string origem, string destino)
{
    // 1. Garantir que destino existe
    // 2. Para cada pasta de ano em origem:
    //    a. Para cada pasta de mês em origem:
    //       - Para cada arquivo PDF:
    //         * Gerar nome único no destino
    //         * Mover arquivo
    // 3. Excluir pasta de origem (vazia)
}
```

### 3. Atualizar `IMapeamentoService`

Arquivo: `src/OrganizadorDocumentos.Core/Services/Interfaces/IMapeamentoService.cs`

- Adicionar método `UnificarColaboradores` na interface
- Adicionar classe `UnificarResultado` para retornar estatísticas

### 4. Atualizar `IFileService`

Arquivo: `src/OrganizadorDocumentos.Core/Services/Interfaces/IFileService.cs`

- Adicionar método `MoverPastaCompleta` na interface

### 5. Atualizar `MapeamentoViewModel`

Arquivo: `src/OrganizadorDocumentos.UI/ViewModels/MapeamentoViewModel.cs`

- Adicionar `ICommand UnificarCommand`
- Adicionar propriedade para exibir o botão
- Implementar handler que abre dialog de seleção

### 6. Atualizar `VisaoGeralView.xaml`

Arquivo: `src/OrganizadorDocumentos.UI/Views/VisaoGeralView.xaml`

- Adicionar botão "[ Unificar ]" acima da lista de colaboradores
- Usar `ListView` com `-packages` para permitir seleção múltipla (ou usar combo box)

### 7. Atualizar `App.xaml.cs`

- Registrar novos services no DI container (já estão registrados, mas verificar se `MapeamentoService` e `FileService` estão sendo injetados)

## Fluxo de Dados

```
[Botão Unificar na View]
    ↓
[MapeamentoViewModel.UnificarCommand]
    ↓
[Dialog: selecionar Colaborador X (origem) e Y (destino)]
    ↓
[MapeamentoService.UnificarColaboradores(X, Y)]
    ↓
[FileService.MoverPastaCompleta(X.CaminhoCompleto, Y.CaminhoCompleto)]
    ↓
[Directory.Delete(X.CaminhoCompleto, true)]
    ↓
[MapeamentoService.AtualizarMapeamento()]
    ↓
[Atualizar UI com nova lista de colaboradores]
```

## Detalhes de Implementação

### `FileService.MoverPastaCompleta`

```csharp
public void MoverPastaCompleta(string origem, string destino)
{
    // 1. Garantir que destino existe
    // 2. Para cada pasta de ano em origem:
    //    a. Para cada pasta de mês em origem:
    //       - Para cada arquivo PDF:
    //         * Gerar nome único no destino
    //         * Mover arquivo
    // 3. Excluir pasta de origem (vazia)
}
```

### `MapeamentoService.UnificarColaboradores`

```csharp
public UnificarResultado UnificarColaboradores(string nomeOrigem, string nomeDestino)
{
    // 1. Validar que ambos existem no mapeamento
    // 2. Obter caminhos completos
    // 3. Chamar FileService.MoverPastaCompleta
    // 4. Atualizar mapeamento
    // 5. Retornar resultado com estatísticas
}
```

## Casos de Teste

1. **Sucesso simples:** Unificar X → Y, onde Y não tem conflitos
2. **Conflito de arquivos:** X e Y têm arquivos com mesmo nome em diferentes meses
3. **Mesmo mês, mesmo ano:** X e Y têm arquivos no mesmo período
4. **Colaborador inexistente:** Tentar unificar com nome que não existe
5. **Unificar consigo mesmo:** Impedir operação idêntica

## Riscos

- **Perda de dados:** Se a operação falhar no meio, o estado fica inconsistente. Solução: usar transação lógica (mover tudo, só excluir origem se tudo OK)
- **Conflitos não resolvidos:** `NomeArquivoUnico` pode gerar nomes confusos. Considerar adicionar sufixo com timestamp
- **Mapeamento desatualizado:** Certificar que `AtualizarMapeamento` é chamado após a unificação

## Próximos Passos

1. Implementar `FileService.MoverPastaCompleta`
2. Implementar `MapeamentoService.UnificarColaboradores`
3. Atualizar `IMapeamentoService` com nova assinatura
4. Atualizar `MapeamentoViewModel` com command e UI
5. Atualizar `VisaoGeralView.xaml` com botão e dialog
6. Testar com os casos de teste listados