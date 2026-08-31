namespace OrganizadorDocumentos.Core.Models;

using OrganizadorDocumentos.Core.Enums;

public class ResultadoProcessamento
{
    public string? ArquivoOrigem { get; set; }
    public string? CaminhoDestino { get; set; }
    public StatusProcessamento Status { get; set; }
    public string? Mensagem { get; set; }
    public DocumentoFinanceiro? DadosExtraidos { get; set; }
    public DateTime ProcessadoEm { get; set; } = DateTime.Now;
}
