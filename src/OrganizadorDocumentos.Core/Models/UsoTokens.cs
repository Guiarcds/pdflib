namespace OrganizadorDocumentos.Core.Models;

/// <summary>
/// Uso de tokens retornado pela API (OpenRouter) em uma requisição.
/// </summary>
public class UsoTokens
{
    /// <summary>Tokens de entrada (inclui tokens de imagem, áudio e tools).</summary>
    public int PromptTokens { get; set; }

    /// <summary>Tokens gerados na resposta.</summary>
    public int CompletionTokens { get; set; }

    /// <summary>Total de tokens (prompt + completion).</summary>
    public int TotalTokens { get; set; }

    /// <summary>Tokens de raciocínio do modelo, quando aplicável.</summary>
    public int ReasoningTokens { get; set; }

    /// <summary>Tokens lidos do cache do provedor.</summary>
    public int CachedTokens { get; set; }

    /// <summary>Custo da requisição em créditos, quando informado pela API.</summary>
    public double? Custo { get; set; }

    public UsoTokens Somar(UsoTokens? outro)
    {
        if (outro == null)
            return new UsoTokens
            {
                PromptTokens = PromptTokens,
                CompletionTokens = CompletionTokens,
                TotalTokens = TotalTokens,
                ReasoningTokens = ReasoningTokens,
                CachedTokens = CachedTokens,
                Custo = Custo
            };

        return new UsoTokens
        {
            PromptTokens = PromptTokens + outro.PromptTokens,
            CompletionTokens = CompletionTokens + outro.CompletionTokens,
            TotalTokens = TotalTokens + outro.TotalTokens,
            ReasoningTokens = ReasoningTokens + outro.ReasoningTokens,
            CachedTokens = CachedTokens + outro.CachedTokens,
            Custo = Custo.HasValue || outro.Custo.HasValue
                ? (Custo ?? 0) + (outro.Custo ?? 0)
                : (double?)null
        };
    }
}
