using StudentFleaMarket.Application.Common.Interfaces;

namespace StudentFleaMarket.API.BackgroundServices;

/// <summary>
/// Tự động hoàn tất các đơn giao dịch quá hạn không có phản hồi sau khi giao / 1 bên đã xác nhận.
/// </summary>
public class TransactionAutoCompleteWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionAutoCompleteWorker> _logger;

    public TransactionAutoCompleteWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionAutoCompleteWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var txService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
                var count = await txService.AutoCompleteExpiredAsync(stoppingToken);
                if (count > 0)
                {
                    _logger.LogInformation("Auto-completed {Count} expired transaction(s).", count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while running TransactionAutoCompleteWorker.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }
}
