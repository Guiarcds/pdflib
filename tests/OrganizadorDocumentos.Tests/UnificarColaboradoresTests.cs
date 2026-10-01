namespace OrganizadorDocumentos.Tests.Services;

using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services;
using OrganizadorDocumentos.Core.Services.Interfaces;
using Xunit;

public class UnificarColaboradoresTests : IDisposable
{
    private readonly string _raiz = Path.Combine(
        Path.GetTempPath(), "OrganizadorDocs_Unificar_" + Guid.NewGuid().ToString("N"));

    private readonly NormalizacaoService _normalizacao = new();
    private readonly FileService _fileService;
    private readonly MapeamentoService _mapeamento;

    public UnificarColaboradoresTests()
    {
        Directory.CreateDirectory(_raiz);
        var log = new LogService(Path.Combine(Path.GetTempPath(), $"unificar_{Guid.NewGuid():N}.log"));
        _fileService = new FileService(_normalizacao, log);
        _mapeamento = new MapeamentoService(_normalizacao, _fileService, log);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
            Directory.Delete(_raiz, true);
    }

    [Fact]
    public void Unificar_SemConflitos_MoveArquivosEExcluiOrigem()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        CriarColaborador("JOAO_SILVA", "2026", "02 - Fevereiro", "VA_JOAO_SILVA_02-2026.pdf");
        CriarColaborador("MARIA_SOUZA", "2025", "05 - Maio", "VT_MARIA_SOUZA_05-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "MARIA_SOUZA");

        Assert.True(resultado.Sucesso, resultado.Mensagem);
        Assert.Equal(2, resultado.ArquivosMovidos);
        Assert.Equal(0, resultado.ConflitosRenomeados);

        Assert.False(Directory.Exists(Path.Combine(_raiz, "COLABORADORES", "JOAO_SILVA")));

        var destino = Path.Combine(_raiz, "COLABORADORES", "MARIA_SOUZA");
        Assert.True(File.Exists(Path.Combine(destino, "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf")));
        Assert.True(File.Exists(Path.Combine(destino, "2026", "02 - Fevereiro", "VA_JOAO_SILVA_02-2026.pdf")));
        Assert.True(File.Exists(Path.Combine(destino, "2025", "05 - Maio", "VT_MARIA_SOUZA_05-2025.pdf")));
    }

    [Fact]
    public void Unificar_AtualizaMapeamento()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        CriarColaborador("MARIA_SOUZA", "2025", "01 - Janeiro", "VT_MARIA_SOUZA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "MARIA_SOUZA");

        Assert.True(resultado.Sucesso, resultado.Mensagem);

        var estrutura = _mapeamento.ObterMapeamento();
        Assert.Equal(1, estrutura.TotalColaboradores);
        Assert.Equal("MARIA_SOUZA", estrutura.Colaboradores[0].NomePasta);
    }

    [Fact]
    public void Unificar_ConflitoDeNome_GeraNomeUnico()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        CriarColaborador("MARIA_SOUZA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "MARIA_SOUZA");

        Assert.True(resultado.Sucesso, resultado.Mensagem);
        Assert.Equal(1, resultado.ArquivosMovidos);
        Assert.Equal(1, resultado.ConflitosRenomeados);

        var mes = Path.Combine(_raiz, "COLABORADORES", "MARIA_SOUZA", "2025", "01 - Janeiro");
        var arquivos = Directory.GetFiles(mes, "*.pdf");
        Assert.Equal(2, arquivos.Length);
        Assert.Contains(arquivos, a => Path.GetFileName(a) == "VT_JOAO_SILVA_01-2025.pdf");
        Assert.Contains(arquivos, a => Path.GetFileName(a) == "VT_JOAO_SILVA_01-2025_1.pdf");
    }

    [Fact]
    public void Unificar_MesmoAnoEMes_ReutilizaPastaExistente()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        CriarColaborador("MARIA_SOUZA", "2025", "01 - Janeiro", "VT_MARIA_SOUZA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "MARIA_SOUZA");

        Assert.True(resultado.Sucesso, resultado.Mensagem);
        Assert.Equal(1, resultado.PastasMesReutilizadas);
        Assert.Equal(0, resultado.PastasMesCriadas);

        var mes = Path.Combine(_raiz, "COLABORADORES", "MARIA_SOUZA", "2025", "01 - Janeiro");
        Assert.Equal(2, Directory.GetFiles(mes, "*.pdf").Length);
    }

    [Fact]
    public void Unificar_ColaboradorInexistente_RetornaFalhaSemAlterarArquivos()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        CriarColaborador("MARIA_SOUZA", "2025", "01 - Janeiro", "VT_MARIA_SOUZA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "FANTASMA");

        Assert.False(resultado.Sucesso);
        Assert.Contains("destino não encontrado", resultado.Mensagem);
        Assert.True(Directory.Exists(Path.Combine(_raiz, "COLABORADORES", "JOAO_SILVA")));
    }

    [Fact]
    public void Unificar_OrigemInexistente_RetornaFalha()
    {
        CriarColaborador("MARIA_SOUZA", "2025", "01 - Janeiro", "VT_MARIA_SOUZA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "MARIA_SOUZA");

        Assert.False(resultado.Sucesso);
        Assert.Contains("origem não encontrado", resultado.Mensagem);
    }

    [Fact]
    public void Unificar_ComMesmoNome_ImpedeOperacao()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_SILVA", "joao_silva");

        Assert.False(resultado.Sucesso);
        Assert.Contains("não podem ser o mesmo", resultado.Mensagem);
        Assert.True(Directory.Exists(Path.Combine(_raiz, "COLABORADORES", "JOAO_SILVA")));
    }

    [Fact]
    public void Unificar_VariacoesDeNomeDeMes_ReuneNaMesmaCompetencia()
    {
        CriarColaborador("JOAO_SILVA", "2025", "01 - Janeiro", "VT_JOAO_SILVA_01-2025.pdf");
        CriarColaborador("JOAO_DA_SILVA", "2025", "Janeiro", "AC_JOAO_DA_SILVA_01-2025.pdf");
        _mapeamento.MapearEstrutura(_raiz);

        var resultado = _mapeamento.UnificarColaboradores("JOAO_DA_SILVA", "JOAO_SILVA");

        Assert.True(resultado.Sucesso, resultado.Mensagem);
        Assert.Equal(1, resultado.ArquivosMovidos);
        Assert.Equal(1, resultado.PastasMesReutilizadas);
        Assert.Equal(0, resultado.ConflitosRenomeados);

        var pastaJoaoSilva = Path.Combine(_raiz, "COLABORADORES", "JOAO_SILVA", "2025");
        Assert.Single(Directory.GetDirectories(pastaJoaoSilva));
        Assert.Equal(2, Directory.GetFiles(pastaJoaoSilva, "*.pdf", SearchOption.AllDirectories).Length);
        Assert.False(Directory.Exists(Path.Combine(_raiz, "COLABORADORES", "JOAO_DA_SILVA")));
    }

    [Fact]
    public void MoverPastaCompleta_OrigemIgualDestino_RetornaErro()
    {
        var caminho = Path.Combine(_raiz, "COLABORADORES", "JOAO_SILVA");
        Directory.CreateDirectory(caminho);

        Assert.Throws<InvalidOperationException>(() => _fileService.MoverPastaCompleta(caminho, caminho));
    }

    [Fact]
    public void MoverPastaCompleta_OrigemInexistente_RetornaErro()
    {
        var inexistente = Path.Combine(_raiz, "COLABORADORES", "NAO_EXISTE");
        var destino = Path.Combine(_raiz, "COLABORADORES", "JOAO_SILVA");
        Directory.CreateDirectory(destino);

        Assert.Throws<DirectoryNotFoundException>(() => _fileService.MoverPastaCompleta(inexistente, destino));
    }

    private void CriarColaborador(string nomePasta, string ano, string mes, params string[] arquivos)
    {
        var pastaMes = Path.Combine(_raiz, "COLABORADORES", nomePasta, ano, mes);
        Directory.CreateDirectory(pastaMes);

        foreach (var arquivo in arquivos)
        {
            File.WriteAllText(Path.Combine(pastaMes, arquivo), "conteudo");
        }
    }
}
