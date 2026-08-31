namespace OrganizadorDocumentos.Core.Models;

public class EstruturaPasta
{
    public List<Colaborador> Colaboradores { get; set; } = new();
    public int TotalAnos => Colaboradores.Sum(c => c.Anos.Count);
    public int TotalPastasMensais => Colaboradores.Sum(c => c.Anos.Sum(a => a.Meses.Count));

    public int TotalColaboradores => Colaboradores.Count;
}
