namespace OrganizadorDocumentos.Core.Services.Interfaces;

public interface IConfiguracaoService
{
    AppConfig CarregarConfiguracao();
    void SalvarConfiguracao(AppConfig config);
    AppConfig ObterConfiguracao();
    void AtualizarConfiguracao(Action<AppConfig> atualizador);
}

public class AppConfig
{
    public string PastaRaiz { get; set; } = string.Empty;
    public string PastaColaboradores { get; set; } = "COLABORADORES";
    public string PastaRevisar { get; set; } = "REVISAR";
    public string PastaEntrada { get; set; } = "ENTRADA";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiModel { get; set; } = "google/gemini-2.0-flash-001";
    public string ModoOperacao { get; set; } = "seguro";
    public double LimiarFuzzy { get; set; } = 0.80;
    public int IntervaloVarreduraMinutos { get; set; } = 30;
    public string MascaraMes { get; set; } = "00 - Mmmm";
}
