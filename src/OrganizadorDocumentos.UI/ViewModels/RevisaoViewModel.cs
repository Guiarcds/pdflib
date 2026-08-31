using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class RevisaoViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogService _logService;

    private ObservableCollection<string> _arquivosRevisar = new();
    public ObservableCollection<string> ArquivosRevisar
    {
        get => _arquivosRevisar;
        set => SetProperty(ref _arquivosRevisar, value);
    }

    private string? _arquivoSelecionado;
    public string? ArquivoSelecionado
    {
        get => _arquivoSelecionado;
        set => SetProperty(ref _arquivoSelecionado, value);
    }

    private string _caminhoPastaRevisar = string.Empty;
    public string CaminhoPastaRevisar
    {
        get => _caminhoPastaRevisar;
        set => SetProperty(ref _caminhoPastaRevisar, value);
    }

    public ICommand AtualizarListaCommand { get; }
    public ICommand AbrirPastaRevisarCommand { get; }

    public RevisaoViewModel(
        IFileService fileService,
        IConfiguracaoService configuracaoService,
        ILogService logService)
    {
        _fileService = fileService;
        _configuracaoService = configuracaoService;
        _logService = logService;

        AtualizarListaCommand = new RelayCommand(_ => AtualizarLista());
        AbrirPastaRevisarCommand = new RelayCommand(_ => AbrirPastaRevisar());

        CarregarPastaRevisar();
    }

    private void CarregarPastaRevisar()
    {
        var config = _configuracaoService.ObterConfiguracao();
        CaminhoPastaRevisar = Path.Combine(config.PastaRaiz, config.PastaRevisar);
        AtualizarLista();
    }

    private void AtualizarLista()
    {
        ArquivosRevisar.Clear();

        if (_fileService.PastaExiste(CaminhoPastaRevisar))
        {
            var pdfs = _fileService.ListarPdfs(CaminhoPastaRevisar);
            foreach (var pdf in pdfs)
            {
                ArquivosRevisar.Add(pdf);
            }
        }
    }

    private void AbrirPastaRevisar()
    {
        if (_fileService.PastaExiste(CaminhoPastaRevisar))
        {
            System.Diagnostics.Process.Start("explorer.exe", CaminhoPastaRevisar);
        }
    }
}
