namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IApiService
{
    Task<List<DocumentoFinanceiro>> ExtrairDadosAsync(string caminhoPdf);
    Task<List<PageDecision>> AnalisarEstruturaPdfAsync(string caminhoPdf);
    Task<DocumentoFinanceiro> ExtrairDadosDocumentoAsync(string caminhoPdf);

    /// <summary>Uso de tokens da última requisição concluída à API.</summary>
    UsoTokens UltimoUso { get; }

    /// <summary>Uso de tokens acumulado desde a inicialização do serviço.</summary>
    UsoTokens UsoAcumulado { get; }

    /// <summary>Disparado a cada requisição concluída, com o uso acumulado.</summary>
    event EventHandler<UsoTokens>? UsoAlterado;

    /// <summary>Zera os contadores de tokens (usado quando a API Key é trocada).</summary>
    void ResetarUso();
}
