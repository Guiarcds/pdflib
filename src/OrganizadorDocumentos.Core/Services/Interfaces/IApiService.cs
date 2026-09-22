namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IApiService
{
    Task<List<DocumentoFinanceiro>> ExtrairDadosAsync(string caminhoPdf);
    Task<List<PageDecision>> AnalisarEstruturaPdfAsync(string caminhoPdf);
    Task<DocumentoFinanceiro> ExtrairDadosDocumentoAsync(string caminhoPdf);
}
