namespace OrganizadorDocumentos.Core.Models;

public class Competencia
{
    public int? Mes { get; set; }
    public int? Ano { get; set; }

    public bool Completa => Mes.HasValue && Ano.HasValue;

    public override string ToString()
    {
        if (Completa)
            return $"{Mes:D2}-{Ano}";
        return "Não identificada";
    }
}
