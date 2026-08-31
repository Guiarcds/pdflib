using System.Windows;
using System.Windows.Controls;
using OrganizadorDocumentos.UI.ViewModels;
using OrganizadorDocumentos.UI.Views;

namespace OrganizadorDocumentos.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DashboardView _dashboardView;
    private readonly MapeamentoView _mapeamentoView;
    private readonly ProcessamentoView _processamentoView;
    private readonly RevisaoView _revisaoView;
    private readonly ConfiguracaoView _configuracaoView;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;

        _dashboardView = new DashboardView { DataContext = _viewModel.DashboardViewModel };
        _mapeamentoView = new MapeamentoView { DataContext = _viewModel.MapeamentoViewModel };
        _processamentoView = new ProcessamentoView { DataContext = _viewModel.ProcessamentoViewModel };
        _revisaoView = new RevisaoView { DataContext = _viewModel.RevisaoViewModel };
        _configuracaoView = new ConfiguracaoView { DataContext = _viewModel.ConfiguracaoViewModel };

        _viewModel.NavegacaoSolicitada += OnNavegacaoSolicitada;
        _viewModel.NavegarDashboardCommand.Execute(null);
    }

    private void OnNavegacaoSolicitada(object? sender, string titulo)
    {
        ContentArea.Content = titulo switch
        {
            "Dashboard" => _dashboardView,
            "Mapeamento" => _mapeamentoView,
            "Processamento" => _processamentoView,
            "Revisão" => _revisaoView,
            "Configurações" => _configuracaoView,
            _ => _dashboardView
        };
    }
}
