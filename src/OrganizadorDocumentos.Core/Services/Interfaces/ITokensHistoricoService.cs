namespace OrganizadorDocumentos.Core.Services.Interfaces;

using OrganizadorDocumentos.Core.Models;

public interface ITokensHistoricoService
{
    /// <summary>Caminho do arquivo de texto com o total de tokens gastos.</summary>
    string CaminhoArquivo { get; }

    /// <summary>Total de tokens já registrado no arquivo de texto.</summary>
    long TotalTokensArquivo { get; }

    /// <summary>Total de custo (US$) já registrado no arquivo de texto.</summary>
    double TotalCustoArquivo { get; }

    /// <summary>
    /// Adiciona o uso informado ao total acumulado no arquivo de texto e grava o resultado.
    /// Retorna o novo total de tokens.
    /// </summary>
    long Registrar(UsoTokens uso);
}
