namespace OrganizadorDocumentos.Tests.Services;

using Xunit;

/// <summary>
/// Garante que o template customizado do ComboBox continue completo.
/// Sem o ToggleButton ligado a IsDropDownOpen o WPF nao abre a lista com o clique
/// do mouse (o ComboBox em si nao trata o clique) e sem PlacementTarget o Popup
/// abre fora do lugar - o que impedia o uso das listas de origem/destino na tela
/// de unificacao de colaboradores.
/// </summary>
public class ComboBoxTemplateTests
{
    private static readonly string Template = Path.Combine(
        LocalizarRaizDoProjeto(), "src", "OrganizadorDocumentos.UI", "Themes", "ComboBox.xaml");

    [Fact]
    public void Template_DeveConterToggleButtonLigadoAoIsDropDownOpen()
    {
        var xaml = File.ReadAllText(Template);

        Assert.Contains("<ToggleButton", xaml, StringComparison.Ordinal);
        Assert.Contains("IsDropDownOpen, Mode=TwoWay", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Template_DeveDefinirPlacementTargetNoPopup()
    {
        var xaml = File.ReadAllText(Template);

        Assert.Contains("PlacementTarget=\"{Binding ElementName=MainGrid}\"", xaml, StringComparison.Ordinal);
    }

    private static string LocalizarRaizDoProjeto()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorio != null)
        {
            if (File.Exists(Path.Combine(diretorio.FullName, "src", "OrganizadorDocumentos.UI", "Themes", "ComboBox.xaml")))
                return diretorio.FullName;

            diretorio = diretorio.Parent;
        }

        throw new DirectoryNotFoundException("Raiz do projeto (Themes/ComboBox.xaml) não encontrada.");
    }
}
