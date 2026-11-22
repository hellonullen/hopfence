using System.Collections.Generic;

namespace HopFence.Core.Models;

/// <summary>
/// Detailed result of a single HopFence diagnostic test.
/// </summary>
public sealed class TestResult
{
    /// <summary>Unique identifier of the test rule (e.g. "HOP-SEC-001").</summary>
    public string TestId { get; set; } = string.Empty;

    /// <summary>Human-readable name of the test.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Summary description of what the test validates.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Final outcome of the test execution.</summary>
    public TestStatus Status { get; set; } = TestStatus.Inconclusive;

    /// <summary>Security severity rating for this test.</summary>
    public TestSeverity Severity { get; set; } = TestSeverity.Info;

    /// <summary>Detailed diagnostic observation and explanation.</summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>Remediation guidance with configuration snippets if misconfiguration is detected.</summary>
    public string? Remediation { get; set; }

    /// <summary>Execution duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Key-value diagnostic metadata captured during execution.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
