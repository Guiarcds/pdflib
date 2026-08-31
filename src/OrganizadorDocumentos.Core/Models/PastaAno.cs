namespace OrganizadorDocumentos.Core.Models;

public class PastaAno
{
    public int Ano { get; set; }
    public string NomePasta { get; set; } = string.Empty;
    public string CaminhoCompleto { get; set; } = string.Empty;
    public List<PastaMes> Meses { get; set; } = new();

    public override string ToString() => Ano.ToString();
}
