namespace OrganizadorDocumentos.Core.Models;

public class Colaborador
{
    public string NomePasta { get; set; } = string.Empty;
    public string NomeNormalizado { get; set; } = string.Empty;
    public string CaminhoCompleto { get; set; } = string.Empty;
    public List<PastaAno> Anos { get; set; } = new();

    public override string ToString() => NomePasta;
}
