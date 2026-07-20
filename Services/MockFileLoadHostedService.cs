namespace LocalMock.Services;

/// <summary>
/// Carrega os mocks do arquivo na inicialização da aplicação.
/// </summary>
public class MockFileLoadHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MockFileLoadHostedService> _logger;

    public MockFileLoadHostedService(IServiceProvider serviceProvider, ILogger<MockFileLoadHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mockService = scope.ServiceProvider.GetRequiredService<IMockService>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar mocks do arquivo na inicialização.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
