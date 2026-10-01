namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface IMapeamentoService
{
    EstruturaPasta MapearEstrutura(string pastaRaiz);
    void AtualizarMapeamento();
    EstruturaPasta ObterMapeamento();
    List<Colaborador> BuscarColaboradoresCompativeis(string nomeNormalizado);
    UnificarResultado UnificarColaboradores(string nomeOrigem, string nomeDestino);
    event EventHandler<MapeamentoEventArgs>? MapeamentoAtualizado;
}

public class MapeamentoEventArgs : EventArgs
{
    public EstruturaPasta Estrutura { get; set; } = new();
}

public class UnificarResultado
{
    public string NomeOrigem { get; set; } = string.Empty;
    public string NomeDestino { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public int ArquivosMovidos { get; set; }
    public int ConflitosRenomeados { get; set; }
    public int PastasMesCriadas { get; set; }
    public int PastasMesReutilizadas { get; set; }
    public List<string> Erros { get; } = new();
    public string Mensagem { get; set; } = string.Empty;
}
