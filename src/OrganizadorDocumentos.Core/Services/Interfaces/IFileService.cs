namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IFileService
{
    bool PastaExiste(string caminho);
    bool ArquivoExiste(string caminho);
    string BuscarPastaAno(int ano, string caminhoColaborador);
    string BuscarPastaMes(int mes, int ano, string caminhoAno);
    void MoverArquivo(string origem, string destino);
    void CriarPasta(string caminho);
    List<string> ListarPdfs(string pasta);
    string NomeArquivoUnico(string caminhoDestino, string nomeBase, string extensao);
}
