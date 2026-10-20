using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OrganizadorDocumentos.Core.Models;

namespace OrganizadorDocumentos.UI.Views;

public partial class VisaoGeralView : UserControl
{
    public VisaoGeralView()
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
        private bool _restaurandoSenha;

        public ConfiguracaoView()
        {
            InitializeComponent();
        }

        private void ConfiguracaoView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.ConfiguracaoViewModel vm && !string.IsNullOrEmpty(vm.ApiKey))
            {
                _restaurandoSenha = true;
                ApiKeyPasswordBox.Password = vm.ApiKey;
                _restaurandoSenha = false;
            }
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_restaurandoSenha)
                return;

            if (DataContext is ViewModels.ConfiguracaoViewModel vm)
            {
                vm.ApiKey = ApiKeyPasswordBox.Password;
            }
        }
}

public partial class UnificarColaboradoresDialog : Window
{
    public string? ColaboradorOrigem { get; private set; }
    public string? ColaboradorDestino { get; private set; }

    public UnificarColaboradoresDialog(IEnumerable<Colaborador> colaboradores)
    {
        InitializeComponent();
        DataContext = colaboradores.OrderBy(c => c.NomePasta).ToList();
    }

    private void OrigemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => AtualizarResumo();

    private void DestinoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => AtualizarResumo();

    private void AtualizarResumo()
    {
        var origem = OrigemCombo.SelectedItem as Colaborador;
        var destino = DestinoCombo.SelectedItem as Colaborador;

        if (origem == null || destino == null)
        {
            ResumoTextBlock.Text = "Selecione os dois colaboradores para ver o resumo da operação.";
            DefinirCorResumo("TextMuted");
            return;
        }

        if (string.Equals(origem.NomePasta, destino.NomePasta, StringComparison.OrdinalIgnoreCase))
        {
            ResumoTextBlock.Text = "A origem e o destino não podem ser o mesmo colaborador.";
            DefinirCorResumo("Warning");
            return;
        }

        ResumoTextBlock.Text =
            $"Origem: {origem.NomePasta} ({origem.Anos.Count} ano(s), {ContarMeses(origem)} pasta(s) de mês)\n" +
            $"Destino: {destino.NomePasta} ({destino.Anos.Count} ano(s), {ContarMeses(destino)} pasta(s) de mês)\n" +
            "Arquivos com o mesmo nome no destino serão mantidos e o arquivo movido será renomeado.";
        DefinirCorResumo("TextPrimary");
    }

    private void DefinirCorResumo(string recurso)
    {
        ResumoTextBlock.Foreground = Application.Current?.TryFindResource(recurso) as Brush ?? Brushes.White;
    }

    private static int ContarMeses(Colaborador colaborador) =>
        colaborador.Anos.Sum(a => a.Meses.Count);

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        var origem = OrigemCombo.SelectedItem as Colaborador;
        var destino = DestinoCombo.SelectedItem as Colaborador;

        if (origem == null || destino == null)
        {
            MessageBox.Show(this,
                "Selecione o colaborador de origem e o de destino.",
                "Unificar colaboradores",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(origem.NomePasta, destino.NomePasta, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this,
                "O colaborador de origem e o de destino não podem ser o mesmo.",
                "Unificar colaboradores",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ColaboradorOrigem = origem.NomePasta;
        ColaboradorDestino = destino.NomePasta;
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Fechar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // a janela ainda não está pronta para ser movida
            }
        }
    }
}
