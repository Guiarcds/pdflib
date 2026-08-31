namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IApiService
{
    Task<DocumentoFinanceiro> ExtrairDadosAsync(string caminhoPdf);
}
