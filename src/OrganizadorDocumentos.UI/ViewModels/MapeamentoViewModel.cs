using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;
using OrganizadorDocumentos.UI.Views;

namespace OrganizadorDocumentos.UI.ViewModels;

public class MapeamentoViewModel : ViewModelBase
{
    private readonly IMapeamentoService _mapeamentoService;
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogService _logService;

    private int _totalColaboradores;
    public int TotalColaboradores
    {
        get => _totalColaboradores;
        set => SetProperty(ref _totalColaboradores, value);
    }

    private bool _mapeando;
    public bool Mapeando
    {
        get => _mapeando;
        set => SetProperty(ref _mapeando, value);
    }

    private string _statusMapeamento = "Aguardando...";
    public string StatusMapeamento
    {
        get => _statusMapeamento;
        set => SetProperty(ref _statusMapeamento, value);
    }

    public ObservableCollection<Colaborador> Colaboradores { get; } = new();

    private bool _podeUnificar;
    public bool PodeUnificar
    {
        get => _podeUnificar;
        set => SetProperty(ref _podeUnificar, value);
    }

    private string _statusUnificacao = string.Empty;
    public string StatusUnificacao
    {
        get => _statusUnificacao;
        set => SetProperty(ref _statusUnificacao, value);
    }

    public ICommand AtualizarEstruturaCommand { get; }
    public ICommand UnificarCommand { get; }

    public MapeamentoViewModel(
        IMapeamentoService mapeamentoService,
        IConfiguracaoService configuracaoService,
        ILogService logService)
    {
        _mapeamentoService = mapeamentoService;
        _configuracaoService = configuracaoService;
        _logService = logService;

        AtualizarEstruturaCommand = new RelayCommand(async _ => await AtualizarEstruturaAsync(), _ => !Mapeando);
        UnificarCommand = new RelayCommand(_ => Unificar());

        _mapeamentoService.MapeamentoAtualizado += OnMapeamentoAtualizado;

        CarregarMapeamentoExistente();
    }

    private void CarregarMapeamentoExistente()
    {
        var estrutura = _mapeamentoService.ObterMapeamento();
        AtualizarExibicao(estrutura);
    }

    private void OnMapeamentoAtualizado(object? sender, MapeamentoEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => AtualizarExibicao(e.Estrutura));
            return;
        }

        AtualizarExibicao(e.Estrutura);
    }

    private async Task AtualizarEstruturaAsync()
    {
        Mapeando = true;
        StatusMapeamento = "Mapeando estrutura...";

        try
        {
            await Task.Run(() =>
            {
                var config = _configuracaoService.ObterConfiguracao();
                if (!string.IsNullOrEmpty(config.PastaRaiz))
                {
                    _mapeamentoService.MapearEstrutura(config.PastaRaiz);
                }
            });

            var estrutura = _mapeamentoService.ObterMapeamento();
            AtualizarExibicao(estrutura);

            StatusMapeamento = $"Mapeamento concluído em {DateTime.Now:HH:mm:ss}";
            _logService.Informacao("Mapeamento atualizado pelo usuário");
        }
        catch (Exception ex)
        {
            StatusMapeamento = $"Erro: {ex.Message}";
            _logService.Erro("Erro ao atualizar mapeamento", ex);
        }
        finally
        {
            Mapeando = false;
        }
    }

    private void AtualizarExibicao(EstruturaPasta estrutura)
    {
        TotalColaboradores = estrutura.TotalColaboradores;

        Colaboradores.Clear();
        foreach (var col in estrutura.Colaboradores.OrderBy(c => c.NomePasta))
        {
            Colaboradores.Add(col);
        }

        PodeUnificar = Colaboradores.Count >= 2;
    }

    private void Unificar()
    {
        SincronizarMapeamento();

        if (Colaboradores.Count < 2)
        {
            StatusUnificacao = "É necessário ter pelo menos dois colaboradores mapeados.";
            _logService.Aviso(StatusUnificacao);
            MostrarAviso(StatusUnificacao);
            return;
        }

        var dialog = new UnificarColaboradoresDialog(Colaboradores)
        {
            Owner = ObterJanelaPrincipal()
        };

        if (dialog.ShowDialog() != true)
        {
            StatusUnificacao = "Unificação cancelada.";
            _logService.Informacao("Unificação cancelada pelo usuário");
            return;
        }

        if (string.IsNullOrWhiteSpace(dialog.ColaboradorOrigem) || string.IsNullOrWhiteSpace(dialog.ColaboradorDestino))
        {
            StatusUnificacao = "Seleção inválida: informe o colaborador de origem e o de destino.";
            _logService.Aviso(StatusUnificacao);
            MostrarAviso(StatusUnificacao);
            return;
        }

        var nomeOrigem = dialog.ColaboradorOrigem;
        var nomeDestino = dialog.ColaboradorDestino;

        var confirmacao = MessageBox.Show(
            ObterJanelaPrincipal(),
            $"Todo o conteúdo de '{nomeOrigem}' será movido para '{nomeDestino}'.\n\n" +
            "A pasta de origem será excluída após a operação. Deseja continuar?",
            "Confirmar unificação",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmacao != MessageBoxResult.Yes)
        {
            StatusUnificacao = "Unificação cancelada.";
            _logService.Informacao("Unificação cancelada na confirmação");
            return;
        }

        StatusUnificacao = $"Unificando '{nomeOrigem}' em '{nomeDestino}'...";
        _logService.Informacao($"Unificando colaborador '{nomeOrigem}' em '{nomeDestino}'");

        var resultado = _mapeamentoService.UnificarColaboradores(nomeOrigem, nomeDestino);

        StatusUnificacao = resultado.Mensagem;

        if (resultado.Sucesso)
            AtualizarExibicao(_mapeamentoService.ObterMapeamento());

        MessageBox.Show(
            ObterJanelaPrincipal(),
            resultado.Mensagem,
            resultado.Sucesso ? "Unificação concluída" : "Unificação não concluída",
            MessageBoxButton.OK,
            resultado.Sucesso ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void SincronizarMapeamento()
    {
        try
        {
            var config = _configuracaoService.ObterConfiguracao();
            if (!string.IsNullOrEmpty(config.PastaRaiz) && Directory.Exists(config.PastaRaiz))
            {
                _mapeamentoService.MapearEstrutura(config.PastaRaiz);
            }
        }
        catch (Exception ex)
        {
            _logService.Erro("Erro ao sincronizar o mapeamento antes da unificação", ex);
        }
    }

    private static Window? ObterJanelaPrincipal()
    {
        var principal = Application.Current?.MainWindow;
        if (principal != null && principal.IsVisible)
            return principal;

        return Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
    }

    private static void MostrarAviso(string mensagem)
    {
        MessageBox.Show(
            ObterJanelaPrincipal(),
            mensagem,
            "Unificar colaboradores",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
