using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class VisaoGeralViewModel : ViewModelBase
{
    private readonly IProcessamentoService _processamentoService;
    private readonly IMapeamentoService _mapeamentoService;
    private readonly IApiService _apiService;
    private readonly ITokensHistoricoService _tokensHistoricoService;

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

    private long _tokensGastos;
    public long TokensGastos
    {
        get => _tokensGastos;
        set => SetProperty(ref _tokensGastos, value);
    }

    private string _custoEstimado = "-";
    public string CustoEstimado
    {
        get => _custoEstimado;
        set => SetProperty(ref _custoEstimado, value);
    }

    private long _tokensHistorico;
    public long TokensHistorico
    {
        get => _tokensHistorico;
        set => SetProperty(ref _tokensHistorico, value);
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
        IApiService apiService,
        ITokensHistoricoService tokensHistoricoService,
        MapeamentoViewModel mapeamentoViewModel)
    {
        _processamentoService = processamentoService;
        _mapeamentoService = mapeamentoService;
        _apiService = apiService;
        _tokensHistoricoService = tokensHistoricoService;
        Mapeamento = mapeamentoViewModel;

        _mapeamentoService.MapeamentoAtualizado += OnMapeamentoAtualizado;
        _apiService.UsoAlterado += OnUsoAlterado;

        AtualizarEstatisticas();
    }

    public void AtualizarEstatisticas()
    {
        _mapeamentoService.ObterMapeamento();
        AtualizarUsoTokens(_apiService.UsoAcumulado);
        AtualizarHistorico();
        UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private void OnMapeamentoAtualizado(object? sender, MapeamentoEventArgs e)
    {
        ExecutarNaInterface(() => AtualizarEstatisticas());
    }

    private void OnUsoAlterado(object? sender, UsoTokens uso)
    {
        ExecutarNaInterface(() =>
        {
            AtualizarUsoTokens(uso);
            AtualizarHistorico();
        });
    }

    private void AtualizarUsoTokens(UsoTokens uso)
    {
        TokensGastos = uso.TotalTokens;
        CustoEstimado = uso.Custo.HasValue ? $"US$ {uso.Custo.Value:0.000000}" : "-";
    }

    private void AtualizarHistorico()
    {
        try
        {
            TokensHistorico = _tokensHistoricoService.TotalTokensArquivo;
        }
        catch (Exception)
        {
            TokensHistorico = 0;
        }
    }

    private static void ExecutarNaInterface(Action acao)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;

        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(acao);
            return;
        }

        acao();
    }
}
