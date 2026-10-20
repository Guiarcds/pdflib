namespace OrganizadorDocumentos.Core.Services;

using System.Globalization;
using System.Text;
using OrganizadorDocumentos.Core.Models;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class TokensHistoricoService : ITokensHistoricoService
{
    private const string NomeArquivo = "tokens_gastos.txt";
    private const string ChaveTokens = "Tokens Gastos";
    private const string ChaveCusto = "Custo Acumulado";
    private const string ChaveAtualizacao = "Ultima Atualizacao";

    private readonly IConfiguracaoService _configuracaoService;
    private readonly ILogService _log;
    private readonly string _pastaPadrao;
    private readonly object _lock = new();

    public TokensHistoricoService(IConfiguracaoService configuracaoService, ILogService log, string pastaPadrao)
    {
        _configuracaoService = configuracaoService;
        _log = log;
        _pastaPadrao = pastaPadrao;
    }

    public string CaminhoArquivo => Path.Combine(ObterPasta(), NomeArquivo);

    public long TotalTokensArquivo
    {
        get
        {
            lock (_lock)
            {
                return LerValor(ChaveTokens);
            }
        }
    }

    public double TotalCustoArquivo
    {
        get
        {
            lock (_lock)
            {
                return LerValorCusto();
            }
        }
    }

    public long Registrar(UsoTokens uso)
    {
        lock (_lock)
        {
            var tokens = LerValor(ChaveTokens) + (uso?.TotalTokens ?? 0);
            var custo = LerValorCusto() + (uso?.Custo ?? 0);

            Gravar(tokens, custo);

            _log.Informacao(
                $"Tokens gastos registrados em '{CaminhoArquivo}': +{uso?.TotalTokens ?? 0} token(s), " +
                $"total {tokens:N0} token(s), custo total US$ {custo:0.000000}");

            return tokens;
        }
    }

    private string ObterPasta()
    {
        string? pastaRaiz = null;

        try
        {
            pastaRaiz = _configuracaoService.ObterConfiguracao().PastaRaiz;
        }
        catch (Exception ex)
        {
            _log.Aviso($"Não foi possível ler a pasta raiz para o histórico de tokens: {ex.Message}");
        }

        if (!string.IsNullOrWhiteSpace(pastaRaiz) && Directory.Exists(pastaRaiz))
            return pastaRaiz;

        if (!string.IsNullOrWhiteSpace(pastaRaiz))
        {
            try
            {
                Directory.CreateDirectory(pastaRaiz);
                return pastaRaiz;
            }
            catch (Exception ex)
            {
                _log.Aviso($"Não foi possível criar a pasta raiz '{pastaRaiz}' para o histórico de tokens: {ex.Message}");
            }
        }

        return _pastaPadrao;
    }

    private long LerValor(string chave)
    {
        try
        {
            if (!File.Exists(CaminhoArquivo))
                return 0;

            foreach (var linha in File.ReadAllLines(CaminhoArquivo, Encoding.UTF8))
            {
                var indice = linha.IndexOf(':');
                if (indice <= 0)
                    continue;

                var identificador = linha[..indice].Trim();
                if (!identificador.Equals(chave, StringComparison.OrdinalIgnoreCase))
                    continue;

                var valor = linha[(indice + 1)..].Replace(".", "", StringComparison.Ordinal)
                    .Replace(" ", string.Empty, StringComparison.Ordinal);

                return long.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultado)
                    ? resultado
                    : 0;
            }
        }
        catch (Exception ex)
        {
            _log.Aviso($"Erro ao ler o histórico de tokens em '{CaminhoArquivo}': {ex.Message}");
        }

        return 0;
    }

    private double LerValorCusto()
    {
        try
        {
            if (!File.Exists(CaminhoArquivo))
                return 0;

            foreach (var linha in File.ReadAllLines(CaminhoArquivo, Encoding.UTF8))
            {
                var indice = linha.IndexOf(':');
                if (indice <= 0)
                    continue;

                var identificador = linha[..indice].Trim();
                if (!identificador.Equals(ChaveCusto, StringComparison.OrdinalIgnoreCase))
                    continue;

                var valor = linha[(indice + 1)..].Trim();
                return double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var resultado)
                    ? resultado
                    : 0;
            }
        }
        catch (Exception ex)
        {
            _log.Aviso($"Erro ao ler o custo acumulado em '{CaminhoArquivo}': {ex.Message}");
        }

        return 0;
    }

    private void Gravar(long tokens, double custo)
    {
        var conteudo = new StringBuilder();
        conteudo.AppendLine("# Organizador de Documentos - Controle de tokens gastos");
        conteudo.AppendLine("# Este arquivo e atualizado automaticamente a cada troca de API Key");
        conteudo.AppendLine($"{ChaveTokens}: {tokens.ToString(CultureInfo.InvariantCulture)}");
        conteudo.AppendLine($"{ChaveCusto}: {custo.ToString("0.000000", CultureInfo.InvariantCulture)}");
        conteudo.AppendLine($"{ChaveAtualizacao}: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

        try
        {
            var pasta = ObterPasta();
            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);

            File.WriteAllText(Path.Combine(pasta, NomeArquivo), conteudo.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _log.Erro($"Erro ao gravar o histórico de tokens em '{CaminhoArquivo}'", ex);
        }
    }
}
