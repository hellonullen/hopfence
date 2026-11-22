using System;
using System.Collections.Generic;
using System.Linq;

namespace HopFence.Core.Models;

/// <summary>
/// Comprehensive summary report produced by HopFence diagnostic run.
/// </summary>
public sealed class DiagnosticReport
{
    /// <summary>Unique ID for this diagnostic session run.</summary>
    public Guid ReportId { get; set; } = Guid.NewGuid();

    /// <summary>Timestamp when diagnostic began.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The base target URL tested.</summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>Detected target framework (e.g. "aspnetcore", "express", "unknown").</summary>
    public string? Framework { get; set; }

    /// <summary>Schema version of the probe report.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Target socket peer IP detected on baseline probe.</summary>
    public string? BaselineSocketPeerIp { get; set; }

    /// <summary>Target client IP detected on baseline probe.</summary>
    public string? BaselineClientIp { get; set; }

    /// <summary>Target scheme detected on baseline probe.</summary>
    public string? BaselineScheme { get; set; }

    /// <summary>Target host detected on baseline probe.</summary>
    public string? BaselineHost { get; set; }

    /// <summary>List of executed test results.</summary>
    public List<TestResult> Results { get; set; } = new();

    /// <summary>Total duration in milliseconds for the whole diagnostic suite.</summary>
    public long TotalDurationMs { get; set; }

    /// <summary>True if zero tests resulted in Fail status.</summary>
    public bool IsSuccess => Results.All(r => r.Status != TestStatus.Fail);

    /// <summary>Count of tests with Pass status.</summary>
    public int TotalPassed => Results.Count(r => r.Status == TestStatus.Pass);

    /// <summary>Count of tests with Fail status.</summary>
    public int TotalFailed => Results.Count(r => r.Status == TestStatus.Fail);

    /// <summary>Count of tests with Warn status.</summary>
    public int TotalWarned => Results.Count(r => r.Status == TestStatus.Warn);

    /// <summary>Count of tests with Inconclusive status.</summary>
    public int TotalInconclusive => Results.Count(r => r.Status == TestStatus.Inconclusive);

    /// <summary>Count of tests with Skip status.</summary>
    public int TotalSkipped => Results.Count(r => r.Status == TestStatus.Skip);
}
