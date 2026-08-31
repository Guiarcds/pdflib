using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IProcessamentoService _processamentoService;
    private readonly IMapeamentoService _mapeamentoService;

    private int _totalProcessados;
    public int TotalProcessados
    {
        get => _totalProcessados;
        set => SetProperty(ref _totalProcessados, value);
    }

    private int _totalRevisar;
    public int TotalRevisar
    {
        get => _totalRevisar;
        set => SetProperty(ref _totalRevisar, value);
    }

    private int _totalErros;
    public int TotalErros
    {
        get => _totalErros;
        set => SetProperty(ref _totalErros, value);
    }

    private string _ultimaAtualizacao = "Nunca";
    public string UltimaAtualizacao
    {
        get => _ultimaAtualizacao;
        set => SetProperty(ref _ultimaAtualizacao, value);
    }

    private bool _processando;
    public bool Processando
    {
        get => _processando;
        set => SetProperty(ref _processando, value);
    }

    public DashboardViewModel(IProcessamentoService processamentoService, IMapeamentoService mapeamentoService)
    {
        _processamentoService = processamentoService;
        _mapeamentoService = mapeamentoService;
        AtualizarEstatisticas();
    }

    public void AtualizarEstatisticas()
    {
        var estrutura = _mapeamentoService.ObterMapeamento();
        UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
