namespace OrganizadorDocumentos.Core.Models;

using OrganizadorDocumentos.Core.Enums;

public class PageDecision
{
    public int Page { get; set; }
    public PageAction Action { get; set; }
    public int Rotation { get; set; }
    public string? Reason { get; set; }
}
