using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Richie.Application.Abstractions;
using Richie.Application.Expenses;

namespace Richie.Infrastructure.Expenses;

/// <summary>
/// Background job that auto-generates due recurring expenses (PRD §7.5). Runs at startup and on
/// a periodic interval; the database is migrated before the host starts, so the first run is safe.
/// </summary>
public sealed class RecurringExpenseProcessingService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    private readonly IExpenseRecurringService _recurring;
    private readonly IClock _clock;
    private readonly ILogger<RecurringExpenseProcessingService> _logger;

    public RecurringExpenseProcessingService(
        IExpenseRecurringService recurring, IClock clock, ILogger<RecurringExpenseProcessingService> logger)
    {
        _recurring = recurring;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int generated = _recurring.ProcessDueRecurring(_clock.UtcNow);
                if (generated > 0)
                    _logger.LogInformation("Auto-generated {Count} recurring expense(s).", generated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring expense processing failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
