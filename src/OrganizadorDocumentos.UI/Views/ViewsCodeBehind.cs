using System.Windows;
using System.Windows.Controls;

namespace OrganizadorDocumentos.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }
}

public partial class MapeamentoView : UserControl
{
    public MapeamentoView()
    {
        InitializeComponent();
    }
}

public partial class ProcessamentoView : UserControl
{
    public ProcessamentoView()
    {
        InitializeComponent();
    }
}

public partial class RevisaoView : UserControl
{
    public RevisaoView()
    {
        InitializeComponent();
    }
}

public partial class ConfiguracaoView : UserControl
{
    public ConfiguracaoView()
    {
        InitializeComponent();
    }

    private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ConfiguracaoViewModel vm)
        {
            vm.ApiKey = ApiKeyPasswordBox.Password;
        }
    }
}
