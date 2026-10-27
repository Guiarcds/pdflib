using System.IO;

namespace OrganizadorDocumentos.Core.Models;

public class Colaborador
{
    public string NomePasta { get; set; } = string.Empty;
    public string NomeNormalizado { get; set; } = string.Empty;
    public string CaminhoCompleto { get; set; } = string.Empty;
    public List<PastaAno> Anos { get; set; } = new();

    public int TotalDocumentos => Directory.Exists(CaminhoCompleto)
        ? Directory.GetFiles(CaminhoCompleto, "*.pdf", SearchOption.AllDirectories).Length
        : 0;

    public override string ToString() => NomePasta;
}
