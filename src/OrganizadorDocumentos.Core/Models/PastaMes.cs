namespace OrganizadorDocumentos.Core.Models;

public class PastaMes
{
    public int NumeroMes { get; set; }
    public string NomePasta { get; set; } = string.Empty;
    public string NomeNormalizado { get; set; } = string.Empty;
    public string CaminhoCompleto { get; set; } = string.Empty;

    public override string ToString() => NomePasta;
}
