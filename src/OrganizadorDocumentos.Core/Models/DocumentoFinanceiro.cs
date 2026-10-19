namespace OrganizadorDocumentos.Core.Models;

using OrganizadorDocumentos.Core.Constants;
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
    public bool TemTipoDocumento => !string.IsNullOrWhiteSpace(TipoDocumento);

    /// <summary>
    /// Sigla preenchida e pertencente ao catálogo de siglas conhecidas (VT, VA, AC, ...).
    /// </summary>
    public bool TemSiglaValida => !string.IsNullOrWhiteSpace(Sigla) && SiglasDocumento.ValidaSigla(Sigla);

    /// <summary>
    /// Documento com tipo conhecido por sigla (recibo/benefício). Quando falso, o documento
    /// é tratado como "não-recibo" e depende do título em <see cref="TipoDocumento"/>.
    /// </summary>
    public bool EhReciboConhecido => TemSiglaValida;
}
