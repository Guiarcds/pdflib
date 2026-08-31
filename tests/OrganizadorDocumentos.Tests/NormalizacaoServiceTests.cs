namespace OrganizadorDocumentos.Tests.Services;

using OrganizadorDocumentos.Core.Services;
using Xunit;

public class NormalizacaoServiceTests
{
    private readonly NormalizacaoService _service = new();

    [Theory]
    [InlineData("João da Silva", "joao da silva")]
    [InlineData("MARIA_DE_SOUZA", "maria_de_souza")]
    [InlineData("Carlos dos Santos", "carlos dos santos")]
    [InlineData("José Álvares", "jose alvares")]
    [InlineData("Conceição", "conceicao")]
    [InlineData("  Espaços  Extras  ", "espacos extras")]
    public void NormalizarNome_RemoveAcentosECaixaAlta(string entrada, string esperado)
    {
        var resultado = _service.NormalizarNome(entrada);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData("João da Silva", "JOAO_DA_SILVA", 0.80)]
    [InlineData("Maria de Souza", "MARIA_DE_SOUZA", 0.80)]
    [InlineData("Carlos Santos", "CARLOS_SANTOS", 0.80)]
    public void CalcularSimilaridade_NomesEquivalentes_RetornaAltaSimilaridade(
        string nome1, string nome2, double minimo)
    {
        var similaridade = _service.CalcularSimilaridade(nome1, nome2);
        Assert.True(similaridade >= minimo, $"Similaridade {similaridade} menor que {minimo}");
    }

    [Fact]
    public void SãoEquivalentes_NomesIguais_RetornaTrue()
    {
        Assert.True(_service.SãoEquivalentes("João da Silva", "JOAO_DA_SILVA"));
    }

    [Fact]
    public void SãoEquivalentes_NomesDiferentes_RetornaFalse()
    {
        Assert.False(_service.SãoEquivalentes("João da Silva", "Maria de Souza"));
    }

    [Theory]
    [InlineData(1, "Janeiro")]
    [InlineData(2, "Fevereiro")]
    [InlineData(8, "Agosto")]
    [InlineData(12, "Dezembro")]
    public void NomeMesPorExtenso_RetornaNomeCorreto(int mes, string esperado)
    {
        var resultado = _service.NomeMesPorExtenso(mes);
        Assert.Equal(esperado, resultado);
    }
}
