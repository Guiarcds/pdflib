# Organizador Inteligente de Documentos Financeiros

Aplicativo para organizar automaticamente PDFs financeiros usando Inteligência Artificial.

## Como Usar

### 1. Iniciar o Aplicativo

Duplo clique em **`Iniciar.bat`** (ou `Compilar e Iniciar.bat` se precisar compilar)

### 2. Configurar (primeira vez)

1. Abra o aplicativo
2. Clique em **Configurações** no menu lateral
3. Configure:
   - **Pasta Raiz**: onde estão os PDFs (ex: `C:\Financeiro` ou `\\servidor\pasta`)
   - **API Key**: sua chave da OpenRouter (https://openrouter.ai)
4. Clique em **Salvar**

### 3. Mapear Pastas

1. Clique em **Mapeamento**
2. Clique em **Atualizar estrutura**
3. O sistema identifica colaboradores, anos e meses existentes

### 4. Processar PDFs

1. Coloque os PDFs na pasta de entrada (padrão: `ENTRADA` dentro da pasta raiz)
2. Clique em **Processamento**
3. Clique em **Processar Agora**

## Estrutura de Pastas

```
Pasta Raiz (você define)
├── PDFS/                    ← Coloque os PDFs aqui
├── COLABORADORES/
│   ├── NOME_COLABORADOR/
│   │   ├── 2026/
│   │   │   ├── 01 - Janeiro/
│   │   │   └── 08 - Agosto/
│   └── OUTRO_COLABORADOR/
└── REVISAR/                 ← Documentos que precisam de revisão
```

## Regras Importantes

- **NUNCA cria pasta duplicada** - se já existe, usa a existente
- **NUNCA mexe em pastas antigas** - só organiza PDFs novos
- **IA só lê documentos** - não cria pastas nem move arquivos
- **ENVIA PARA REVISAR** quando tem dúvida

## Siglas dos Documentos

| Sigla | Tipo |
|-------|------|
| VT | Vale Transporte |
| VA | Vale Alimentação |
| AC | Ajuda de Custo |
| BO | Bonificação |
| CO | Comissão |
| SP | Serviço Prestado |
| DE | Diária |
| SE | Salário Extra |
| SB | Salário Base |
| OS | Vale por OS |

## Nome do Arquivo

```
SIGLA_Nome_Colaborador_MM-YYYY.pdf

Exemplo: VT_Joao_da_Silva_08-2026.pdf
```

## Arquivos do Projeto

| Arquivo | Função |
|---------|--------|
| `Iniciar.bat` | Executa o aplicativo |
| `Compilar e Iniciar.bat` | Compila e executa |
| `bin/` | Pasta com o .exe compilado |
| `src/` | Código fonte |
| `tests/` | Testes automatizados |
