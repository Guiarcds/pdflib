using System.Windows;
using System.Windows.Controls;
using OrganizadorDocumentos.UI.ViewModels;
using OrganizadorDocumentos.UI.Views;

namespace OrganizadorDocumentos.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly VisaoGeralView _visaoGeralView;
    private readonly ProcessamentoView _processamentoView;
    private readonly RevisaoView _revisaoView;
    private readonly ConfiguracaoView _configuracaoView;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;

        _visaoGeralView = new VisaoGeralView { DataContext = _viewModel.VisaoGeralViewModel };
        _processamentoView = new ProcessamentoView { DataContext = _viewModel.ProcessamentoViewModel };
        _revisaoView = new RevisaoView { DataContext = _viewModel.RevisaoViewModel };
        _configuracaoView = new ConfiguracaoView { DataContext = _viewModel.ConfiguracaoViewModel };

        _viewModel.NavegacaoSolicitada += OnNavegacaoSolicitada;
        _viewModel.NavegarVisaoGeralCommand.Execute(null);
    }

    private void OnNavegacaoSolicitada(object? sender, string titulo)
    {
        ContentArea.Content = titulo switch
        {
            "Visão Geral" => _visaoGeralView,
            "Processamento" => _processamentoView,
            "Revisão" => _revisaoView,
            "Configurações" => _configuracaoView,
            _ => _visaoGeralView
        };
    }
}
