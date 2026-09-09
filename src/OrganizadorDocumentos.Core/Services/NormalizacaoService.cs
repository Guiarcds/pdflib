namespace OrganizadorDocumentos.Core.Services;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FuzzySharp;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class NormalizacaoService : INormalizacaoService
{
    private static readonly Dictionary<char, char> MapaAcentos = new()
    {
        { 'à', 'a' }, { 'á', 'a' }, { 'â', 'a' }, { 'ã', 'a' }, { 'ä', 'a' },
        { 'è', 'e' }, { 'é', 'e' }, { 'ê', 'e' }, { 'ë', 'e' },
        { 'ì', 'i' }, { 'í', 'i' }, { 'î', 'i' }, { 'ï', 'i' },
        { 'ò', 'o' }, { 'ó', 'o' }, { 'ô', 'o' }, { 'õ', 'o' }, { 'ö', 'o' },
        { 'ù', 'u' }, { 'ú', 'u' }, { 'û', 'u' }, { 'ü', 'u' },
        { 'ç', 'c' }, { 'ñ', 'n' },
        { 'ý', 'y' }, { 'ÿ', 'y' }
    };

    private static readonly Dictionary<int, string> NomesMeses = new()
    {
        { 1, "Janeiro" },
        { 2, "Fevereiro" },
        { 3, "Março" },
        { 4, "Abril" },
        { 5, "Maio" },
        { 6, "Junho" },
        { 7, "Julho" },
        { 8, "Agosto" },
        { 9, "Setembro" },
        { 10, "Outubro" },
        { 11, "Novembro" },
        { 12, "Dezembro" }
    };

    public string NormalizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        var sb = new StringBuilder(nome.Length);

        foreach (var c in nome.ToLowerInvariant())
        {
            if (MapaAcentos.TryGetValue(c, out var substituto))
                sb.Append(substituto);
            else
                sb.Append(c);
        }

        var normalizado = sb.ToString();
        normalizado = normalizado.Replace('_', ' '); // Converte underscores para espaços
        normalizado = Regex.Replace(normalizado, @"[^a-z0-9\s]", "");
        normalizado = Regex.Replace(normalizado, @"\s+", " ").Trim();

        // Remove partículas comuns que não ajudam na diferenciação
        var particulas = new[] { " da ", " de ", " do ", " das ", " dos ", " e " };
        foreach (var p in particulas)
        {
            normalizado = normalizado.Replace(p, " ");
        }
        normalizado = Regex.Replace(normalizado, @"\s+", " ").Trim();

        return normalizado;
    }

    public double CalcularSimilaridade(string nome1, string nome2)
    {
        if (string.IsNullOrWhiteSpace(nome1) || string.IsNullOrWhiteSpace(nome2))
            return 0.0;

        var norm1 = NormalizarNome(nome1);
        var norm2 = NormalizarNome(nome2);

        if (norm1 == norm2)
            return 1.0;

        // Usa múltiplos algoritmos e pega o melhor
        var ratio = Fuzz.Ratio(norm1, norm2);
        var partialRatio = Fuzz.PartialRatio(norm1, norm2);
        var tokenSortRatio = Fuzz.TokenSortRatio(norm1, norm2);
        var tokenSetRatio = Fuzz.TokenSetRatio(norm1, norm2);

        var melhorScore = new[] { ratio, partialRatio, tokenSortRatio, tokenSetRatio }.Max();
        return melhorScore / 100.0;
    }

    public bool SãoEquivalentes(string nome1, string nome2, double limiar = 0.85)
    {
        return CalcularSimilaridade(nome1, nome2) >= limiar;
    }

    public string NomeMesPorExtenso(int mes)
    {
        return NomesMeses.TryGetValue(mes, out var nome) ? nome : string.Empty;
    }
}
