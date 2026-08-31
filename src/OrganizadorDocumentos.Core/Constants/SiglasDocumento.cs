namespace OrganizadorDocumentos.Core.Constants;

public static class SiglasDocumento
{
    public const string VT = "VT";
    public const string VA = "VA";
    public const string AC = "AC";
    public const string BO = "BO";
    public const string CO = "CO";
    public const string SP = "SP";
    public const string DE = "DE";
    public const string SE = "SE";
    public const string SB = "SB";
    public const string OS = "OS";

    public static readonly Dictionary<string, string> Descricas = new()
    {
        { VT, "Vale Transporte" },
        { VA, "Vale Alimentação" },
        { AC, "Ajuda de Custo" },
        { BO, "Bonificação" },
        { CO, "Comissão" },
        { SP, "Serviço Prestado" },
        { DE, "Diária / Empreita" },
        { SE, "Salário Extra" },
        { SB, "Salário Base" },
        { OS, "Vale feito em troca de OS" }
    };

    public static bool ValidaSigla(string sigla)
    {
        return Descricas.ContainsKey(sigla?.ToUpperInvariant() ?? string.Empty);
    }

    public static string ObterDescricao(string sigla)
    {
        return Descricas.TryGetValue(sigla?.ToUpperInvariant() ?? string.Empty, out var descricao)
            ? descricao
            : string.Empty;
    }
}
