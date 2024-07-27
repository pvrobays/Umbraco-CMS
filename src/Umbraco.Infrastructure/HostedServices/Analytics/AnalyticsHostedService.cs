using System.Diagnostics;
using Humanizer;
using Humanizer.Bytes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.Cms.Infrastructure.HostedServices.Analytics;

/// <summary>
/// Represents the analytics hosted service
/// </summary>
public class AnalyticsHostedService : BackgroundService
{
    private readonly ILogger<AnalyticsHostedService> _logger;
    private readonly IOptionsMonitor<AnalyticsConfig> _analyticsConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsHostedService"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="analyticsConfig"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AnalyticsHostedService(
        ILogger<AnalyticsHostedService> logger,
        IOptionsMonitor<AnalyticsConfig> analyticsConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analyticsConfig = analyticsConfig ?? throw new ArgumentNullException(nameof(analyticsConfig));
    }

    /// <summary>
    /// Executes the background service
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime startTime = DateTime.UtcNow;
            var currentProcess = Process.GetCurrentProcess();
            TimeSpan startCpuUsage = currentProcess.TotalProcessorTime;

            var currentValueLogIntervalInMs = _analyticsConfig.CurrentValue.LogIntervalInMs ?? 60_000;
            await Task.Delay(TimeSpan.FromMilliseconds(currentValueLogIntervalInMs), stoppingToken);

            DateTime endTime = DateTime.UtcNow;
            TimeSpan endCpuUsage = currentProcess.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            var cpuUsage = (cpuUsageTotal * 100).ToString("0.00") + "%";
            var memoryUsage = ByteSize.FromBytes(currentProcess.WorkingSet64).Humanize("#");

            _logger.LogInformation("CPU Usage = {CpUsage}, Memory Usage = {MemoryUsage}", cpuUsage, memoryUsage);
        }
    }
}
