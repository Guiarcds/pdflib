using System.Windows.Input;
using Microsoft.Win32;
using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class ConfiguracaoViewModel : ViewModelBase
{
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogService _logService;

    private string _pastaRaiz = string.Empty;
    public string PastaRaiz
    {
        get => _pastaRaiz;
        set => SetProperty(ref _pastaRaiz, value);
    }

    private string _pastaColaboradores = "COLABORADORES";
    public string PastaColaboradores
    {
        get => _pastaColaboradores;
        set => SetProperty(ref _pastaColaboradores, value);
    }

    private string _pastaRevisar = "REVISAR";
    public string PastaRevisar
    {
        get => _pastaRevisar;
        set => SetProperty(ref _pastaRevisar, value);
    }

    private string _pastaEntrada = "ENTRADA";
    public string PastaEntrada
    {
        get => _pastaEntrada;
        set => SetProperty(ref _pastaEntrada, value);
    }

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    private string _apiModel = "google/gemini-2.0-flash-001";
    public string ApiModel
    {
        get => _apiModel;
        set => SetProperty(ref _apiModel, value);
    }

    private string _modoOperacao = "seguro";
    public string ModoOperacao
    {
        get => _modoOperacao;
        set => SetProperty(ref _modoOperacao, value);
    }

    private double _limiarFuzzy = 0.80;
    public double LimiarFuzzy
    {
        get => _limiarFuzzy;
        set => SetProperty(ref _limiarFuzzy, value);
    }

    private string _mascaraMes = "00 - Mmmm";
    public string MascaraMes
    {
        get => _mascaraMes;
        set => SetProperty(ref _mascaraMes, value);
    }

    private string _statusSalvamento = string.Empty;
    public string StatusSalvamento
    {
        get => _statusSalvamento;
        set => SetProperty(ref _statusSalvamento, value);
    }

    public ICommand SelecionarPastaCommand { get; }
    public ICommand SalvarCommand { get; }

    public ConfiguracaoViewModel(IConfiguracaoService configuracaoService, ILogService logService)
    {
        _configuracaoService = configuracaoService;
        _logService = logService;

        SelecionarPastaCommand = new RelayCommand(_ => SelecionarPasta());
        SalvarCommand = new RelayCommand(_ => SalvarConfiguracao());

        CarregarConfiguracao();
    }

    private void CarregarConfiguracao()
    {
        var config = _configuracaoService.ObterConfiguracao();
        PastaRaiz = config.PastaRaiz;
        PastaColaboradores = config.PastaColaboradores;
        PastaRevisar = config.PastaRevisar;
        PastaEntrada = config.PastaEntrada;
        ApiKey = config.ApiKey;
        ApiModel = config.ApiModel;
        ModoOperacao = config.ModoOperacao;
        LimiarFuzzy = config.LimiarFuzzy;
        MascaraMes = config.MascaraMes;
    }

    private void SelecionarPasta()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecione a pasta raiz do financeiro",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            PastaRaiz = dialog.FolderName;
        }
    }

    private void SalvarConfiguracao()
    {
        try
        {
            _configuracaoService.AtualizarConfiguracao(config =>
            {
                config.PastaRaiz = PastaRaiz;
                config.PastaColaboradores = PastaColaboradores;
                config.PastaRevisar = PastaRevisar;
                config.PastaEntrada = PastaEntrada;
                config.ApiKey = ApiKey;
                config.ApiModel = ApiModel;
                config.ModoOperacao = ModoOperacao;
                config.LimiarFuzzy = LimiarFuzzy;
                config.MascaraMes = MascaraMes;
            });

            StatusSalvamento = $"Configurações salvas em {DateTime.Now:HH:mm:ss}";
            _logService.Informacao("Configurações atualizadas pelo usuário");
        }
        catch (Exception ex)
        {
            StatusSalvamento = $"Erro ao salvar: {ex.Message}";
            _logService.Erro("Erro ao salvar configurações", ex);
        }
    }
}
