using Languag.io.Infrastructure.AiDeckGeneration;

namespace Languag.io.Worker;

public class AiDeckGenerationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiDeckGenerationWorker> _logger;

    public AiDeckGenerationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AiDeckGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI deck generation worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<AiDeckGenerationProcessor>();
                var processedJob = await processor.ProcessNextPendingJobAsync(stoppingToken);
                var delay = processedJob ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5);

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI deck generation worker loop.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
