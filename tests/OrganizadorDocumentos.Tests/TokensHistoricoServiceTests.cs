namespace OrganizadorDocumentos.Tests.Services;

using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services;
using OrganizadorDocumentos.Core.Services.Interfaces;
using Xunit;

public class TokensHistoricoServiceTests : IDisposable
{
    private readonly string _pastaTeste = Path.Combine(Path.GetTempPath(), "OrganizadorDocs_Tokens_" + Guid.NewGuid().ToString("N"));
    private readonly string _pastaPadrao;
    private readonly string _caminhoLog = Path.Combine(Path.GetTempPath(), $"OrganizadorDocs_TokensLog_{Guid.NewGuid():N}.log");

    public TokensHistoricoServiceTests()
    {
        Directory.CreateDirectory(_pastaTeste);
        _pastaPadrao = Path.Combine(_pastaTeste, "padrao");
        Directory.CreateDirectory(_pastaPadrao);
    }

    void IDisposable.Dispose()
    {
        if (Directory.Exists(_pastaTeste))
            Directory.Delete(_pastaTeste, true);
    }

    [Fact]
    public void Registrar_PrimeiroUso_CriaArquivoNaPastaRaiz()
    {
        var servico = CriarServico();

        var total = servico.Registrar(new UsoTokens { TotalTokens = 1200, Custo = 0.0015 });

        Assert.Equal(1200, total);
        Assert.True(File.Exists(servico.CaminhoArquivo));
        Assert.Equal(Path.Combine(_pastaTeste, "tokens_gastos.txt"), servico.CaminhoArquivo);
        Assert.Contains("Tokens Gastos: 1200", File.ReadAllText(servico.CaminhoArquivo));
    }

    [Fact]
    public void Registrar_ChamadasSequenciais_AcumulaNoArquivo()
    {
        var servico = CriarServico();

        servico.Registrar(new UsoTokens { TotalTokens = 1000, Custo = 0.001 });
        servico.Registrar(new UsoTokens { TotalTokens = 500, Custo = 0.002 });
        var total = servico.Registrar(new UsoTokens { TotalTokens = 250, Custo = 0.0005 });

        Assert.Equal(1750, total);
        Assert.Equal(1750, servico.TotalTokensArquivo);
        Assert.Equal(0.0035, servico.TotalCustoArquivo, 5);
        Assert.Contains("Tokens Gastos: 1750", File.ReadAllText(servico.CaminhoArquivo));
    }

    [Fact]
    public void Registrar_SemPastaRaizConfigurada_UsaPastaPadrao()
    {
        var configuracao = new TestConfiguracaoService(new AppConfig { PastaRaiz = string.Empty });
        var log = new LogService(_caminhoLog);
        var servico = new TokensHistoricoService(configuracao, log, _pastaPadrao);

        servico.Registrar(new UsoTokens { TotalTokens = 10 });

        Assert.Equal(Path.Combine(_pastaPadrao, "tokens_gastos.txt"), servico.CaminhoArquivo);
        Assert.True(File.Exists(servico.CaminhoArquivo));
    }

    [Fact]
    public void TotalTokensArquivo_SemArquivo_RetornaZero()
    {
        var servico = CriarServico();

        Assert.Equal(0, servico.TotalTokensArquivo);
        Assert.Equal(0, servico.TotalCustoArquivo);
    }

    private ITokensHistoricoService CriarServico()
    {
        var configuracao = new TestConfiguracaoService(new AppConfig { PastaRaiz = _pastaTeste });
        var log = new LogService(_caminhoLog);
        return new TokensHistoricoService(configuracao, log, _pastaPadrao);
    }
}
