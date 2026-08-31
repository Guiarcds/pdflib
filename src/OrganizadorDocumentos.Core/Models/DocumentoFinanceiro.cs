namespace OrganizadorDocumentos.Core.Models;

using OrganizadorDocumentos.Core.Enums;

public class DocumentoFinanceiro
{
    public string? Colaborador { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Sigla { get; set; }
    public Competencia? Competencia { get; set; }
    public string? Data { get; set; }
    public string? NumeroOS { get; set; }
    public double Confianca { get; set; }

    public bool TemColaborador => !string.IsNullOrWhiteSpace(Colaborador);
    public bool TemSigla => !string.IsNullOrWhiteSpace(Sigla);
    public bool TemCompetencia => Competencia?.Completa == true;
}
