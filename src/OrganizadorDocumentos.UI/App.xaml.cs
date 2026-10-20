using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OrganizadorDocumentos.Core.Services;
using OrganizadorDocumentos.Core.Services.Interfaces;
using OrganizadorDocumentos.UI.ViewModels;

namespace OrganizadorDocumentos.UI;

public partial class App : Application
{
    private ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OrganizadorDocumentos");

        Directory.CreateDirectory(appDataPath);
        Directory.CreateDirectory(Path.Combine(appDataPath, "logs"));

        var configPath = Path.Combine(appDataPath, "config.json");
        var logPath = Path.Combine(appDataPath, "logs", $"log_{DateTime.Now:yyyy-MM-dd}.txt");

        var configuracaoService = new ConfiguracaoService(configPath);
        var logService = new LogService(logPath);

        services.AddSingleton<IConfiguracaoService>(configuracaoService);
        services.AddSingleton<ILogService>(logService);
        services.AddSingleton<ITokensHistoricoService>(new TokensHistoricoService(configuracaoService, logService, appDataPath));
        services.AddSingleton<INormalizacaoService, NormalizacaoService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IMapeamentoService, MapeamentoService>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<IProcessamentoService, ProcessamentoService>();

        services.AddSingleton<MapeamentoViewModel>();
        services.AddSingleton<VisaoGeralViewModel>();
        services.AddSingleton<ProcessamentoViewModel>();
        services.AddSingleton<RevisaoViewModel>();
        services.AddSingleton<ConfiguracaoViewModel>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            var apiService = _serviceProvider?.GetService<IApiService>();
            var tokensHistorico = _serviceProvider?.GetService<ITokensHistoricoService>();

            if (apiService != null && tokensHistorico != null && apiService.UsoAcumulado.TotalTokens > 0)
            {
                tokensHistorico.Registrar(apiService.UsoAcumulado);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao registrar tokens na saída: {ex.Message}");
        }

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
