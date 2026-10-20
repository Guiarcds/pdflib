namespace OrganizadorDocumentos.Tests.Services;

using OrganizadorDocumentos.Core.Enums;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services;
using OrganizadorDocumentos.Core.Services.Interfaces;
using Xunit;

public class ProcessamentoServiceTests : IDisposable
{
    private readonly string _pastaRaiz = Path.Combine(Path.GetTempPath(), "OrganizadorDocs_ProcessamentoTests_" + Guid.NewGuid().ToString("N"));

    public ProcessamentoServiceTests()
    {
        Directory.CreateDirectory(_pastaRaiz);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pastaRaiz))
                Directory.Delete(_pastaRaiz, true);
        }
        catch
        {
            // limpeza best-effort
        }
    }

    [Fact]
    public async Task ProcessarDocumento_SemSiglaComTipoDocumento_UsaTituloComoPrefixo()
    {
        var documento = CriarDocumento(sigla: null, tipoDocumento: "Termo de Contrato");
        var service = CriarService(documento);

        var resultado = await service.ProcessarDocumentoAsync("origem.pdf");

        Assert.Equal(StatusProcessamento.Sucesso, resultado.Status);
        Assert.Equal("TERMO_DE_CONTRATO_João_da_Silva_09-2026.pdf", Path.GetFileName(resultado.CaminhoDestino));
    }

    [Fact]
    public async Task ProcessarDocumento_SemSiglaNemTipoDocumento_EnviaParaRevisao()
    {
        var documento = CriarDocumento(sigla: null, tipoDocumento: null);
        var service = CriarService(documento);

        var resultado = await service.ProcessarDocumentoAsync("origem.pdf");

        Assert.Equal(StatusProcessamento.Revisar, resultado.Status);
        Assert.Contains("Tipo de documento não identificado", resultado.Mensagem);
    }

    [Fact]
    public async Task ProcessarDocumento_ComSiglaValida_UsaSiglaComoPrefixo()
    {
        var documento = CriarDocumento(sigla: "VT", tipoDocumento: "Vale Transporte");
        var service = CriarService(documento);

        var resultado = await service.ProcessarDocumentoAsync("origem.pdf");

        Assert.Equal(StatusProcessamento.Sucesso, resultado.Status);
        Assert.Equal("VT_João_da_Silva_09-2026.pdf", Path.GetFileName(resultado.CaminhoDestino));
    }

    [Fact]
    public async Task ProcessarDocumento_ComSiglaDesconhecida_UsaTipoDocumentoComoPrefixo()
    {
        var documento = CriarDocumento(sigla: "XX", tipoDocumento: "Recibo de Pagamento");
        var service = CriarService(documento);

        var resultado = await service.ProcessarDocumentoAsync("origem.pdf");

        Assert.Equal(StatusProcessamento.Sucesso, resultado.Status);
        Assert.Equal("RECIBO_DE_PAGAMENTO_João_da_Silva_09-2026.pdf", Path.GetFileName(resultado.CaminhoDestino));
    }

    [Fact]
    public async Task ProcessarDocumento_TipoDocumentoComCaracteresInvalidos_SanitizaNomeArquivo()
    {
        var documento = CriarDocumento(sigla: null, tipoDocumento: "Termo de Contrato / Assinatura: 2026");
        var service = CriarService(documento);

        var resultado = await service.ProcessarDocumentoAsync("origem.pdf");

        Assert.Equal(StatusProcessamento.Sucesso, resultado.Status);
        Assert.Equal("TERMO_DE_CONTRATO_ASSINATURA_2026_João_da_Silva_09-2026.pdf", Path.GetFileName(resultado.CaminhoDestino));
    }

    [Fact]
    public async Task ProcessarDocumento_SiglaOSComNumero_MantemNomeOriginal()
    {
        var documento = CriarDocumento(sigla: "OS", tipoDocumento: "Ordem de Serviço");
        documento.NumeroOS = "12345";
        var service = CriarService(documento);

        var resultado = await service.ProcessarDocumentoAsync("origem.pdf");

        Assert.Equal(StatusProcessamento.Sucesso, resultado.Status);
        Assert.Equal("OS_João_da_Silva_OS-12345.pdf", Path.GetFileName(resultado.CaminhoDestino));
    }

    private static DocumentoFinanceiro CriarDocumento(string? sigla, string? tipoDocumento)
    {
        return new DocumentoFinanceiro
        {
            Colaborador = "João da Silva",
            Sigla = sigla,
            TipoDocumento = tipoDocumento,
            Competencia = new Competencia { Mes = 9, Ano = 2026 },
            Confianca = 0.9
        };
    }

    private ProcessamentoService CriarService(DocumentoFinanceiro documento)
    {
        var configuracao = new TestConfiguracaoService(new AppConfig
        {
            ApiKey = "test-key",
            ApiModel = "test-model",
            PastaRaiz = _pastaRaiz,
            PastaRevisar = "REVISAR"
        });

        var mapeamento = new FakeMapeamentoService
        {
            Colaboradores =
            {
                new Colaborador
                {
                    NomePasta = "JOAO_DA_SILVA",
                    NomeNormalizado = "joao silva",
                    CaminhoCompleto = Path.Combine(_pastaRaiz, "COLABORADORES", "JOAO_DA_SILVA")
                }
            }
        };

        return new ProcessamentoService(
            new FakeApiService { Documento = documento },
            new FakeFileService(),
            mapeamento,
            new NormalizacaoService(),
            configuracao,
            new FakeLogService());
    }

    private class FakeApiService : IApiService
    {
        public DocumentoFinanceiro Documento { get; set; } = new();

        public UsoTokens UltimoUso { get; } = new();

        public UsoTokens UsoAcumulado { get; } = new();

        public event EventHandler<UsoTokens>? UsoAlterado
        {
            add { }
            remove { }
        }

        public void ResetarUso()
        {
        }

        public Task<List<DocumentoFinanceiro>> ExtrairDadosAsync(string caminhoPdf)
            => Task.FromResult(new List<DocumentoFinanceiro> { Documento });

        public Task<List<PageDecision>> AnalisarEstruturaPdfAsync(string caminhoPdf)
            => Task.FromResult(new List<PageDecision>
            {
                new PageDecision { Page = 1, Action = PageAction.Keep, Rotation = 0 }
            });

        public Task<DocumentoFinanceiro> ExtrairDadosDocumentoAsync(string caminhoPdf)
            => Task.FromResult(Documento);
    }

    private class FakeFileService : IFileService
    {
        public List<(string Origem, string Destino)> Movicoes { get; } = new();

        public bool PastaExiste(string caminho) => true;
        public bool ArquivoExiste(string caminho) => false;
        public string BuscarPastaAno(int ano, string caminhoColaborador) => Path.Combine(caminhoColaborador, ano.ToString());
        public string BuscarPastaMes(int mes, int ano, string caminhoAno) => Path.Combine(caminhoAno, $"{mes:D2}");
        public void MoverArquivo(string origem, string destino, bool sobrescrever = false) => Movicoes.Add((origem, destino));
        public void CriarPasta(string caminho) { }
        public List<string> ListarPdfs(string pasta) => new();
        public string NomeArquivoUnico(string caminhoDestino, string nomeBase, string extensao) => Path.Combine(caminhoDestino, nomeBase + extensao);
        public ResultadoMoverPasta MoverPastaCompleta(string origem, string destino) => new();

        public Task<List<string>> DividirPdfAsync(string caminhoPdf, List<DocumentoFinanceiro> documentos, string pastaSaida)
            => Task.FromResult(new List<string>());

        public Task<List<string>> AplicarCorrecoesAsync(string caminhoPdf, List<PageDecision> decisoes, string pastaTemp)
            => Task.FromResult(new List<string> { Path.Combine(pastaTemp, "pagina_1.jpg") });

        public Task<List<string>> SplitUmaPaginaPorPdfAsync(List<string> imagensCorrigidas, string pastaTemp)
            => Task.FromResult(new List<string> { Path.Combine(pastaTemp, "documento_1.pdf") });

        public Task<byte[]?> RenderizarPaginaPdfAsync(string caminhoPdf, int pagina = 0, int densidade = 200)
            => Task.FromResult<byte[]?>(null);
    }

    private class FakeMapeamentoService : IMapeamentoService
    {
        public List<Colaborador> Colaboradores { get; } = new();

        public event EventHandler<MapeamentoEventArgs>? MapeamentoAtualizado
        {
            add { }
            remove { }
        }

        public EstruturaPasta MapearEstrutura(string pastaRaiz) => ObterMapeamento();
        public void AtualizarMapeamento() { }
        public EstruturaPasta ObterMapeamento() => new EstruturaPasta { Colaboradores = Colaboradores };
        public List<Colaborador> BuscarColaboradoresCompativeis(string nomeNormalizado) => Colaboradores;
        public UnificarResultado UnificarColaboradores(string nomeOrigem, string nomeDestino) => new();
    }

    private class FakeLogService : ILogService
    {
        public void Informacao(string mensagem) { }
        public void Aviso(string mensagem) { }
        public void Erro(string mensagem, Exception? excecao = null) { }
        public void Debug(string mensagem) { }
    }
}
