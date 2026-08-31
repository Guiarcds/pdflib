namespace OrganizadorDocumentos.Core.Services.Interfaces;

public interface ILogService
{
    void Informacao(string mensagem);
    void Aviso(string mensagem);
    void Erro(string mensagem, Exception? excecao = null);
    void Debug(string mensagem);
}
