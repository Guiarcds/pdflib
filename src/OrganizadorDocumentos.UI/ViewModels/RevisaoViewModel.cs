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
    private readonly IMapeamentoService _mapeamentoService;
    private readonly INormalizacaoService _normalizacaoService;

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
        set
        {
            if (SetProperty(ref _arquivoSelecionado, value))
            {
                CarregarDadosArquivo(value);
            }
        }
    }

    private string _caminhoPastaRevisar = string.Empty;
    public string CaminhoPastaRevisar
    {
        get => _caminhoPastaRevisar;
        set => SetProperty(ref _caminhoPastaRevisar, value);
    }

    // Campos do formulário de revisão manual
    private string _colaborador = string.Empty;
    public string Colaborador
    {
        get => _colaborador;
        set => SetProperty(ref _colaborador, value);
    }

    private string _sigla = string.Empty;
    public string Sigla
    {
        get => _sigla;
        set => SetProperty(ref _sigla, value);
    }

    private int? _competenciaMes;
    public int? CompetenciaMes
    {
        get => _competenciaMes;
        set => SetProperty(ref _competenciaMes, value);
    }

    private int? _competenciaAno;
    public int? CompetenciaAno
    {
        get => _competenciaAno;
        set => SetProperty(ref _competenciaAno, value);
    }

    private string _data = string.Empty;
    public string Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }

    private string _numeroOS = string.Empty;
    public string NumeroOS
    {
        get => _numeroOS;
        set => SetProperty(ref _numeroOS, value);
    }

    private string _statusMensagem = string.Empty;
    public string StatusMensagem
    {
        get => _statusMensagem;
        set => SetProperty(ref _statusMensagem, value);
    }

    private bool _temArquivoSelecionado;
    public bool TemArquivoSelecionado
    {
        get => _temArquivoSelecionado;
        set => SetProperty(ref _temArquivoSelecionado, value);
    }

    private readonly string[] _siglasValidas = new[] { "VT", "VA", "AC", "BO", "CO", "SP", "DE", "SE", "SB", "OS" };

    public ICommand AtualizarListaCommand { get; }
    public ICommand AbrirPastaRevisarCommand { get; }
    public ICommand ProcessarManualCommand { get; }
    public ICommand LimparFormularioCommand { get; }

    public RevisaoViewModel(
        IFileService fileService,
        IConfiguracaoService configuracaoService,
        ILogService logService,
        IMapeamentoService mapeamentoService,
        INormalizacaoService normalizacaoService)
    {
        _fileService = fileService;
        _configuracaoService = configuracaoService;
        _logService = logService;
        _mapeamentoService = mapeamentoService;
        _normalizacaoService = normalizacaoService;

        AtualizarListaCommand = new RelayCommand(_ => AtualizarLista());
        AbrirPastaRevisarCommand = new RelayCommand(_ => AbrirPastaRevisar());
        ProcessarManualCommand = new RelayCommand(_ => ProcessarManual(), _ => PodeProcessar());
        LimparFormularioCommand = new RelayCommand(_ => LimparFormulario());

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

    private void CarregarDadosArquivo(string? caminhoArquivo)
    {
        TemArquivoSelecionado = !string.IsNullOrEmpty(caminhoArquivo) && File.Exists(caminhoArquivo);
        
        if (!TemArquivoSelecionado)
        {
            LimparFormulario();
            return;
        }

        // Preenche competência padrão (mês/ano atual)
        CompetenciaMes = DateTime.Now.Month;
        CompetenciaAno = DateTime.Now.Year;

        StatusMensagem = "Preencha os campos abaixo e clique em Processar";
    }

    private bool PodeProcessar()
    {
        return TemArquivoSelecionado 
            && !string.IsNullOrWhiteSpace(Colaborador)
            && !string.IsNullOrWhiteSpace(Sigla)
            && _siglasValidas.Contains(Sigla.ToUpperInvariant())
            && CompetenciaMes.HasValue && CompetenciaMes.Value >= 1 && CompetenciaMes.Value <= 12
            && CompetenciaAno.HasValue && CompetenciaAno.Value >= 2000 && CompetenciaAno.Value <= 2100;
    }

    private void ProcessarManual()
    {
        if (!PodeProcessar() || string.IsNullOrEmpty(ArquivoSelecionado))
        {
            StatusMensagem = "Preencha todos os campos obrigatórios corretamente";
            return;
        }

        try
        {
            var config = _configuracaoService.ObterConfiguracao();
            var siglaUpper = Sigla.ToUpperInvariant();

            // Verifica/cria pasta do colaborador
            var estrutura = _mapeamentoService.ObterMapeamento();
            var nomeNormalizado = _normalizacaoService.NormalizarNome(Colaborador);
            var colaboradoresCompativeis = _mapeamentoService.BuscarColaboradoresCompativeis(nomeNormalizado);

            string pastaColaborador;
            if (colaboradoresCompativeis.Count == 0)
            {
                // Cria novo colaborador
                var pastaColaboradores = Path.Combine(config.PastaRaiz, config.PastaColaboradores);
                var nomePasta = Colaborador.ToUpperInvariant()
                    .Replace(" ", "_")
                    .Replace("-", "_");
                pastaColaborador = Path.Combine(pastaColaboradores, nomePasta);
                _fileService.CriarPasta(pastaColaborador);
                
                // Atualiza mapeamento
                _mapeamentoService.AtualizarMapeamento();
            }
            else if (colaboradoresCompativeis.Count > 1)
            {
                StatusMensagem = $"Múltiplos colaboradores compatíveis: {string.Join(", ", colaboradoresCompativeis.Select(c => c.NomePasta))}";
                return;
            }
            else
            {
                pastaColaborador = colaboradoresCompativeis[0].CaminhoCompleto;
            }

            // Busca/cria pasta ano/mês
            var pastaAno = _fileService.BuscarPastaAno(CompetenciaAno.Value, pastaColaborador);
            var pastaMes = _fileService.BuscarPastaMes(CompetenciaMes.Value, CompetenciaAno.Value, pastaAno);

            // Gera nome do arquivo
            var nomeArquivo = GerarNomeArquivo(siglaUpper, Colaborador, CompetenciaMes.Value, CompetenciaAno.Value, NumeroOS);
            var caminhoDestino = Path.Combine(pastaMes, nomeArquivo);

            if (_fileService.ArquivoExiste(caminhoDestino))
            {
                StatusMensagem = $"Arquivo já existe no destino: {nomeArquivo}";
                return;
            }

            // Move o arquivo
            _fileService.MoverArquivo(ArquivoSelecionado, caminhoDestino);

            _logService.Informacao($"Documento processado manualmente: {Path.GetFileName(ArquivoSelecionado)} -> {caminhoDestino}");
            
            StatusMensagem = $"✓ Processado com sucesso: {nomeArquivo}";
            
            // Remove da lista e limpa formulário
            ArquivosRevisar.Remove(ArquivoSelecionado);
            LimparFormulario();
        }
        catch (Exception ex)
        {
            _logService.Erro($"Erro ao processar manualmente: {ex.Message}", ex);
            StatusMensagem = $"Erro: {ex.Message}";
        }
    }

    private void LimparFormulario()
    {
        Colaborador = string.Empty;
        Sigla = string.Empty;
        CompetenciaMes = DateTime.Now.Month;
        CompetenciaAno = DateTime.Now.Year;
        Data = string.Empty;
        NumeroOS = string.Empty;
        StatusMensagem = string.Empty;
        TemArquivoSelecionado = false;
    }

    private string GerarNomeArquivo(string sigla, string colaborador, int mes, int ano, string numeroOS)
    {
        var nomeColaborador = colaborador.Replace(" ", "_");

        if (sigla == "OS" && !string.IsNullOrWhiteSpace(numeroOS))
        {
            return $"OS_{nomeColaborador}_OS-{numeroOS}.pdf";
        }

        return $"{sigla}_{nomeColaborador}_{mes:D2}-{ano}.pdf";
    }
}
