using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class VisaoGeralViewModel : ViewModelBase
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

    public MapeamentoViewModel Mapeamento { get; }

    public VisaoGeralViewModel(
        IProcessamentoService processamentoService,
        IMapeamentoService mapeamentoService,
        MapeamentoViewModel mapeamentoViewModel)
    {
        _processamentoService = processamentoService;
        _mapeamentoService = mapeamentoService;
        Mapeamento = mapeamentoViewModel;

        _mapeamentoService.MapeamentoAtualizado += OnMapeamentoAtualizado;

        AtualizarEstatisticas();
    }

    public void AtualizarEstatisticas()
    {
        _mapeamentoService.ObterMapeamento();
        UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private void OnMapeamentoAtualizado(object? sender, MapeamentoEventArgs e) => AtualizarEstatisticas();
}
