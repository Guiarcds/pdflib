namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IProcessamentoService
{
    Task<ResultadoProcessamento> ProcessarDocumentoAsync(string caminhoPdf);
    Task<List<ResultadoProcessamento>> ProcessarLoteAsync(List<string> arquivos, IProgress<ProgressoProcessamento>? progress = null);
    event EventHandler<ResultadoProcessamento>? DocumentoProcessado;
}

public class ProgressoProcessamento
{
    public int Total { get; set; }
    public int Processados { get; set; }
    public string? ArquivoAtual { get; set; }
    public double Percentual => Total > 0 ? (double)Processados / Total * 100 : 0;
}
