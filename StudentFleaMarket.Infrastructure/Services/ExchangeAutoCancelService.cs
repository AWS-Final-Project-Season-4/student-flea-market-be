using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentFleaMarket.Application.Common.Interfaces;

namespace StudentFleaMarket.Infrastructure.Services;

/// <summary>
/// BACKGROUND JOB — TỰ HỦY ĐƠN ĐỔI ĐỒ QUÁ HẠN.
///
/// VÌ SAO CẦN JOB NÀY:
///   Luồng đổi đồ bắt buộc cả 2 bên xác nhận bàn giao mới hoàn thành. Nếu một
///   bên không xuất hiện (bùng hàng, đổi ý, quên), đơn sẽ treo ở trạng thái
///   "Chờ xác nhận" vô thời hạn và giữ chính món đồ — chặn người khác mua.
///
///   Vì vậy: tính từ thời điểm gặp mặt đã chốt, quá 7 ngày mà chưa đủ 2 bên
///   xác nhận thì đơn bị hủy tự động và các tin đăng trả lại "đang bán".
///
/// VÌ SAO DÙNG JOB THAY VÌ CHECK "LAZY":
///   Job chạy định kỳ nên đơn bị hủy đúng hạn, không phụ thuộc người dùng có
///   mở app hay không. Check lazy chỉ chạy khi có request -> đơn có thể bị giữ
///   chiếm thêm nhiều ngày nếu không ai vào app.
///
/// LỊCH CHẠY:
///   Mỗi 1 giờ. Hạn tính theo ngày nên việc chạy mỗi giờ là đủ — độ trễ
///   tối đa ~1 giờ, không đáng kể so với hạn 7 ngày.
///
/// AN TOÀN:
///   - Lỗi trong 1 lần chạy không làm sập cả ứng dụng (try/catch + log).
///   - Dùng CancellationToken của host để dừng graceful khi API shutdown.
/// </summary>
public class ExchangeAutoCancelService : BackgroundService
{
    /// <summary>
    /// Chu kỳ quét đơn quá hạn (giờ).
    /// </summary>
    private const int IntervalHours = 1;

    /// <summary>
    /// Độ trễ khởi động lần đầu (giây) — chờ app ổn định rồi mới chạy.
    /// </summary>
    private const int StartupDelaySeconds = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExchangeAutoCancelService> _logger;

    public ExchangeAutoCancelService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExchangeAutoCancelService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Background job tự hủy đơn đổi đồ quá hạn đã khởi động (quét mỗi {Hours} giờ).",
            IntervalHours);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(StartupDelaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(IntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host đang shutdown -> dừng bình thường.
                break;
            }
            catch (Exception ex)
            {
                // Lỗi 1 lần chạy KHÔNG được làm sập ứng dụng.
                _logger.LogError(ex, "Background job tự hủy đơn quá hạn gặp lỗi — sẽ thử lại ở vòng sau.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Background job tự hủy đơn quá hạn đã dừng.");
    }

    /// <summary>
    /// Chạy 1 vòng quét: tìm đơn quá hạn và hủy.
    /// </summary>
    private async Task RunOnceAsync(CancellationToken ct)
    {
        // Mỗi vòng cần scope mới: service giao dịch là Scoped, không dùng được
        // cho background job (job sống lâu hơn 1 scope).
        using var scope = _scopeFactory.CreateScope();

        var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

        var cancelledCount = await transactionService.AutoCancelExpiredAsync(ct);

        if (cancelledCount > 0)
            _logger.LogInformation(
                "Đã tự hủy {Count} đơn đổi đồ quá hạn xác nhận bàn giao.",
                cancelledCount);
    }
}
