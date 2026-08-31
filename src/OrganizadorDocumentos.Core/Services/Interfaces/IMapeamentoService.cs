namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IMapeamentoService
{
    EstruturaPasta MapearEstrutura(string pastaRaiz);
    void AtualizarMapeamento();
    EstruturaPasta ObterMapeamento();
    List<Colaborador> BuscarColaboradoresCompativeis(string nomeNormalizado);
    event EventHandler<MapeamentoEventArgs>? MapeamentoAtualizado;
}

public class MapeamentoEventArgs : EventArgs
{
    public EstruturaPasta Estrutura { get; set; } = new();
}
