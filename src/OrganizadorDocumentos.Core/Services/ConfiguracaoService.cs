namespace OrganizadorDocumentos.Core.Services;

using Newtonsoft.Json;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class ConfiguracaoService : IConfiguracaoService
{
    private readonly string _caminhoArquivo;
    private AppConfig? _configuracao;

    public ConfiguracaoService(string caminhoArquivo)
    {
        _caminhoArquivo = caminhoArquivo;
    }

    public AppConfig CarregarConfiguracao()
    {
        if (_configuracao != null)
            return _configuracao;

        if (File.Exists(_caminhoArquivo))
        {
            var json = File.ReadAllText(_caminhoArquivo);
            _configuracao = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
        }
        else
        {
            _configuracao = new AppConfig();
            SalvarConfiguracao(_configuracao);
        }

        return _configuracao;
    }

    public void SalvarConfiguracao(AppConfig config)
    {
        var diretorio = Path.GetDirectoryName(_caminhoArquivo);
        if (!string.IsNullOrEmpty(diretorio) && !Directory.Exists(diretorio))
            Directory.CreateDirectory(diretorio);

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(_caminhoArquivo, json);
        _configuracao = config;
    }

    public AppConfig ObterConfiguracao()
    {
        return CarregarConfiguracao();
    }

    public void AtualizarConfiguracao(Action<AppConfig> atualizador)
    {
        var config = ObterConfiguracao();
        atualizador(config);
        SalvarConfiguracao(config);
    }
}
