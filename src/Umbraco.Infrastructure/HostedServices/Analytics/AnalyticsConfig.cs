namespace Umbraco.Cms.Infrastructure.HostedServices.Analytics;

/// <summary>
/// Represents the configuration for the analytics hosted service
/// </summary>
public class AnalyticsConfig
{
    /// <summary>
    /// Specifies how often analytics should be logged
    /// </summary>
    public int? LogIntervalInMs { get; set; }

    /// <summary>
    /// Specifies how long the application should sleep on startup
    /// </summary>
    public int? SleepOnStartupInMs { get; set; }
}
