using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using OrganizadorDocumentos.Core.Enums;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

namespace OrganizadorDocumentos.UI.ViewModels;

public class ProcessamentoViewModel : ViewModelBase
{
    private readonly IProcessamentoService _processamentoService;
    private readonly IFileService _fileService;
    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogService _logService;

    private bool _processando;
    public bool Processando
    {
        get => _processando;
        set => SetProperty(ref _processando, value);
    }

    private int _progresso;
    public int Progresso
    {
        get => _progresso;
        set => SetProperty(ref _progresso, value);
    }

    private string _arquivoAtual = string.Empty;
    public string ArquivoAtual
    {
        get => _arquivoAtual;
        set => SetProperty(ref _arquivoAtual, value);
    }

    private int _totalArquivos;
    public int TotalArquivos
    {
        get => _totalArquivos;
        set => SetProperty(ref _totalArquivos, value);
    }

    private int _processados;
    public int Processados
    {
        get => _processados;
        set => SetProperty(ref _processados, value);
    }

    private int _revisar;
    public int Revisar
    {
        get => _revisar;
        set => SetProperty(ref _revisar, value);
    }

    private int _erros;
    public int Erros
    {
        get => _erros;
        set => SetProperty(ref _erros, value);
    }

    public ObservableCollection<ResultadoProcessamento> Resultados { get; } = new();
    public ObservableCollection<string> ArquivosPendentes { get; } = new();

    public ICommand ProcessarCommand { get; }
    public ICommand AtualizarListaCommand { get; }

    public ProcessamentoViewModel(
        IProcessamentoService processamentoService,
        IFileService fileService,
        IConfiguracaoService configuracaoService,
        ILogService logService)
    {
        _processamentoService = processamentoService;
        _fileService = fileService;
        _configuracaoService = configuracaoService;
        _logService = logService;

        ProcessarCommand = new RelayCommand(async _ => await ProcessarAsync(), _ => !Processando);
        AtualizarListaCommand = new RelayCommand(_ => AtualizarListaArquivos());

        AtualizarListaArquivos();
    }

    private void AtualizarListaArquivos()
    {
        ArquivosPendentes.Clear();
        var config = _configuracaoService.ObterConfiguracao();
        var pastaEntrada = Path.Combine(config.PastaRaiz, config.PastaEntrada);

        if (_fileService.PastaExiste(pastaEntrada))
        {
            var pdfs = _fileService.ListarPdfs(pastaEntrada);
            foreach (var pdf in pdfs)
            {
                ArquivosPendentes.Add(pdf);
            }
            TotalArquivos = pdfs.Count;
        }
    }

    private async Task ProcessarAsync()
    {
        var config = _configuracaoService.ObterConfiguracao();
        var pastaEntrada = Path.Combine(config.PastaRaiz, config.PastaEntrada);
        var arquivos = _fileService.ListarPdfs(pastaEntrada);

        if (arquivos.Count == 0)
        {
            _logService.Aviso("Nenhum arquivo PDF encontrado na pasta de entrada");
            return;
        }

        Processando = true;
        Processados = 0;
        Revisar = 0;
        Erros = 0;
        Resultados.Clear();

        var progress = new Progress<ProgressoProcessamento>(p =>
        {
            Progresso = (int)p.Percentual;
            ArquivoAtual = p.ArquivoAtual ?? string.Empty;
        });

        try
        {
            var resultados = await _processamentoService.ProcessarLoteAsync(arquivos, progress);

            foreach (var resultado in resultados)
            {
                Resultados.Add(resultado);
                Processados++;

                if (resultado.Status == StatusProcessamento.Revisar)
                    Revisar++;
                else if (resultado.Status == StatusProcessamento.Erro)
                    Erros++;
            }

            _logService.Informacao($"Processamento concluído: {Processados} processados, {Revisar} em revisão, {Erros} erros");
        }
        catch (Exception ex)
        {
            _logService.Erro("Erro durante processamento em lote", ex);
        }
        finally
        {
            Processando = false;
            AtualizarListaArquivos();
        }
    }
}
