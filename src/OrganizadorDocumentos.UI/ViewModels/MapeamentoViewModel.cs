using System.Collections.ObjectModel;
using System.Windows.Input;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

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

    private int _totalAnos;
    public int TotalAnos
    {
        get => _totalAnos;
        set => SetProperty(ref _totalAnos, value);
    }

    private int _totalPastasMensais;
    public int TotalPastasMensais
    {
        get => _totalPastasMensais;
        set => SetProperty(ref _totalPastasMensais, value);
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

    public ICommand AtualizarEstruturaCommand { get; }

    public MapeamentoViewModel(
        IMapeamentoService mapeamentoService,
        IConfiguracaoService configuracaoService,
        ILogService logService)
    {
        _mapeamentoService = mapeamentoService;
        _configuracaoService = configuracaoService;
        _logService = logService;

        AtualizarEstruturaCommand = new RelayCommand(async _ => await AtualizarEstruturaAsync(), _ => !Mapeando);

        CarregarMapeamentoExistente();
    }

    private void CarregarMapeamentoExistente()
    {
        var estrutura = _mapeamentoService.ObterMapeamento();
        AtualizarExibicao(estrutura);
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
        TotalAnos = estrutura.TotalAnos;
        TotalPastasMensais = estrutura.TotalPastasMensais;

        Colaboradores.Clear();
        foreach (var col in estrutura.Colaboradores.OrderBy(c => c.NomePasta))
        {
            Colaboradores.Add(col);
        }
    }
}
