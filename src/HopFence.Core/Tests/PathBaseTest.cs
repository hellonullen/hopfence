using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test G: Validates whether the application preserves subpath / PathBase routing behind reverse proxy.
/// </summary>
public sealed class PathBaseTest : IDiagnosticTest
{
    public string Id => "HOP-PATHBASE-001";
    public string Name => "Subpath / PathBase Preservation";
    public string Description => "Checks whether the backend correctly preserves PathBase when hosted behind a subpath prefix.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.PathBase;

    public async Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var probeResult = await context.Client.ExecuteProbeAsync(cancellationToken: cancellationToken);
        sw.Stop();

        if (!probeResult.IsSuccessStatusCode || probeResult.ProbeData == null)
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Inconclusive,
                Severity = TestSeverity.Medium,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"Probe request failed with HTTP {(int)probeResult.StatusCode}: {probeResult.ErrorMessage ?? probeResult.RawBody}"
            };
        }

        string? detectedPathBase = probeResult.ProbeData.Request?.PathBase ?? string.Empty;
        string? expectedPathBase = context.Config.Expect.PathBase;

        if (!string.IsNullOrWhiteSpace(expectedPathBase))
        {
            if (string.Equals(detectedPathBase.TrimEnd('/'), expectedPathBase.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                return new TestResult
                {
                    TestId = Id,
                    Name = Name,
                    Description = Description,
                    Status = TestStatus.Pass,
                    Severity = TestSeverity.Info,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = $"PathBase matched expected prefix '{expectedPathBase}'.",
                    Metadata = { ["DetectedPathBase"] = detectedPathBase, ["ExpectedPathBase"] = expectedPathBase }
                };
            }

            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Fail,
                Severity = TestSeverity.Medium,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"Detected PathBase '{detectedPathBase}' does not match expected prefix '{expectedPathBase}'.",
                Remediation = "In ASP.NET Core: Call 'app.UsePathBase(\"/subpath\")'. In Nginx: Verify proxy_pass URL path trimming/appending rules.",
                Metadata = { ["DetectedPathBase"] = detectedPathBase, ["ExpectedPathBase"] = expectedPathBase }
            };
        }

        return new TestResult
        {
            TestId = Id,
            Name = Name,
            Description = Description,
            Status = TestStatus.Pass,
            Severity = TestSeverity.Info,
            DurationMs = sw.ElapsedMilliseconds,
            Details = string.IsNullOrWhiteSpace(detectedPathBase)
                ? "No subpath prefix detected (Application running at root path '/')."
                : $"Application running with PathBase: '{detectedPathBase}'.",
            Metadata = { ["DetectedPathBase"] = detectedPathBase }
        };
    }
}
