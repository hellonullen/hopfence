using System;

namespace HopFence.Reporting;

/// <summary>
/// Factory for resolving HopFence report formatters.
/// </summary>
public static class ReporterFactory
{
    public static IHopFenceReporter GetReporter(string? format)
    {
        string normalized = (format ?? "console").Trim().ToLowerInvariant();
        return normalized switch
        {
            "json" => new JsonReporter(),
            "html" => new HtmlReporter(),
            "junit" or "xml" => new JUnitReporter(),
            _ => new ConsoleReporter()
        };
    }
}
