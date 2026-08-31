namespace OrganizadorDocumentos.Core.Services;

using Serilog;
using OrganizadorDocumentos.Core.Services.Interfaces;

public class LogService : ILogService
{
    private readonly ILogger _logger;

    public LogService(string caminhoLog)
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                caminhoLog,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public void Informacao(string mensagem) => _logger.Information(mensagem);
    public void Aviso(string mensagem) => _logger.Warning(mensagem);
    public void Erro(string mensagem, Exception? excecao = null)
    {
        if (excecao != null)
            _logger.Error(excecao, mensagem);
        else
            _logger.Error(mensagem);
    }
    public void Debug(string mensagem) => _logger.Debug(mensagem);
}
