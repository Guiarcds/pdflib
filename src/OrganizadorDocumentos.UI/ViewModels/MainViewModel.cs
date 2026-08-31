using System.Windows.Input;
using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IConfiguracaoService _configuracaoService;
    private readonly IMapeamentoService _mapeamentoService;
    private readonly IProcessamentoService _processamentoService;
    private readonly ILogService _logService;

    private string _titulo = "Organizador Inteligente de Documentos Financeiros";
    public string Titulo
    {
        get => _titulo;
        set => SetProperty(ref _titulo, value);
    }

    private string _statusSistema = "Pronto";
    public string StatusSistema
    {
        get => _statusSistema;
        set => SetProperty(ref _statusSistema, value);
    }

    public ICommand NavegarDashboardCommand { get; }
    public ICommand NavegarMapeamentoCommand { get; }
    public ICommand NavegarProcessamentoCommand { get; }
    public ICommand NavegarRevisaoCommand { get; }
    public ICommand NavegarConfiguracaoCommand { get; }

    public DashboardViewModel DashboardViewModel { get; }
    public MapeamentoViewModel MapeamentoViewModel { get; }
    public ProcessamentoViewModel ProcessamentoViewModel { get; }
    public RevisaoViewModel RevisaoViewModel { get; }
    public ConfiguracaoViewModel ConfiguracaoViewModel { get; }

    public event EventHandler<string>? NavegacaoSolicitada;

    public MainViewModel(
        IConfiguracaoService configuracaoService,
        IMapeamentoService mapeamentoService,
        IProcessamentoService processamentoService,
        ILogService logService,
        DashboardViewModel dashboardViewModel,
        MapeamentoViewModel mapeamentoViewModel,
        ProcessamentoViewModel processamentoViewModel,
        RevisaoViewModel revisaoViewModel,
        ConfiguracaoViewModel configuracaoViewModel)
    {
        _configuracaoService = configuracaoService;
        _mapeamentoService = mapeamentoService;
        _processamentoService = processamentoService;
        _logService = logService;

        DashboardViewModel = dashboardViewModel;
        MapeamentoViewModel = mapeamentoViewModel;
        ProcessamentoViewModel = processamentoViewModel;
        RevisaoViewModel = revisaoViewModel;
        ConfiguracaoViewModel = configuracaoViewModel;

        NavegarDashboardCommand = new RelayCommand(_ => NavegarPara("Dashboard"));
        NavegarMapeamentoCommand = new RelayCommand(_ => NavegarPara("Mapeamento"));
        NavegarProcessamentoCommand = new RelayCommand(_ => NavegarPara("Processamento"));
        NavegarRevisaoCommand = new RelayCommand(_ => NavegarPara("Revisão"));
        NavegarConfiguracaoCommand = new RelayCommand(_ => NavegarPara("Configurações"));
    }

    private void NavegarPara(string titulo)
    {
        Titulo = $"Organizador de Documentos - {titulo}";
        NavegacaoSolicitada?.Invoke(this, titulo);
    }
}
