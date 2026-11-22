using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test A: Verifies that the backend application correctly perceives the request scheme as HTTPS.
/// </summary>
public sealed class HttpsDetectionTest : IDiagnosticTest
{
    public string Id => "HOP-HTTPS-001";
    public string Name => "HTTPS Protocol Detection";
    public string Description => "Verifies that the backend perceives the request scheme as HTTPS behind reverse proxy.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.HttpsDetection;

    public async Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        string expectedScheme = context.Config.Expect.Scheme ?? "https";

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
                Details = $"Probe request failed with HTTP {(int)probeResult.StatusCode}: {probeResult.ErrorMessage ?? probeResult.RawBody}",
                Remediation = "Verify that the target application has HopFence middleware installed and HOPFENCE_ENABLED=true."
            };
        }

        string? detectedScheme = probeResult.ProbeData.Request?.Scheme;

        if (string.Equals(detectedScheme, expectedScheme, StringComparison.OrdinalIgnoreCase))
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Pass,
                Severity = TestSeverity.Info,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"Backend successfully recognized request scheme as '{detectedScheme}'.",
                Metadata = { ["DetectedScheme"] = detectedScheme ?? "", ["ExpectedScheme"] = expectedScheme }
            };
        }

        return new TestResult
        {
            TestId = Id,
            Name = Name,
            Description = Description,
            Status = TestStatus.Fail,
            Severity = TestSeverity.High,
            DurationMs = sw.ElapsedMilliseconds,
            Details = $"Backend perceived request scheme as '{detectedScheme}' instead of expected '{expectedScheme}'. " +
                      "This will cause broken HTTPS redirects, insecure cookie generation, and SSL termination misdetection.",
            Remediation = "In Nginx: Add 'proxy_set_header X-Forwarded-Proto $scheme;'. In ASP.NET Core: Call 'app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });'. In Express: Set 'app.set(\"trust proxy\", 1);'."
        };
    }
}
