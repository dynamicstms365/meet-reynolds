using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace CopilotAgent.Telemetry;

/// <summary>
/// Reynolds: Maximum Effort™ telemetry configuration with proper DI integration
/// </summary>
public class ReynoldsTelemetryConfiguration : IConfigureOptions<TelemetryConfiguration>
{
    private readonly ReynoldsTelemetryInitializer _telemetryInitializer;

    public ReynoldsTelemetryConfiguration(ReynoldsTelemetryInitializer telemetryInitializer)
    {
        _telemetryInitializer = telemetryInitializer;
    }

    public void Configure(TelemetryConfiguration options)
    {
        options.TelemetryInitializers.Add(_telemetryInitializer);
    }
}