namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface INormalizacaoService
{
    string NormalizarNome(string nome);
    double CalcularSimilaridade(string nome1, string nome2);
    bool SãoEquivalentes(string nome1, string nome2, double limiar = 0.80);
    string NomeMesPorExtenso(int mes);
}
