using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test E: Tests whether an attacker can poison the application Host header via X-Forwarded-Host.
/// </summary>
public sealed class ForwardedHostSpoofingTest : IDiagnosticTest
{
    public const string SpoofedHost = "attacker.hopfence.invalid";

    public string Id => "HOP-SPOOF-HOST-001";
    public string Name => "X-Forwarded-Host Poisoning Protection";
    public string Description => "Injects an untrusted 'X-Forwarded-Host: attacker.hopfence.invalid' header to test for Host header injection.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.ForwardedHostSpoofing;

    public async Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var customHeaders = new Dictionary<string, string>
        {
            ["X-Forwarded-Host"] = SpoofedHost
        };

        var probeResult = await context.Client.ExecuteProbeAsync(customHeaders, cancellationToken);
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

        string? perceivedHost = probeResult.ProbeData.Request?.Host;

        if (!string.IsNullOrEmpty(perceivedHost) &&
            perceivedHost.Contains(SpoofedHost, StringComparison.OrdinalIgnoreCase))
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Fail,
                Severity = TestSeverity.High,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"VULNERABILITY: Application accepted untrusted Host '{perceivedHost}' injected via X-Forwarded-Host! " +
                          "This exposes the application to Host Header Poisoning, password-reset token theft, and web cache poisoning.",
                Remediation = "1. In Nginx: Set 'proxy_set_header X-Forwarded-Host $host;' or 'proxy_set_header Host $host;' explicitly.\n" +
                              "2. In ASP.NET Core: Configure HostFilteringMiddleware with explicit AllowedHosts in appsettings.json.\n" +
                              "3. Do not forward client-supplied X-Forwarded-Host headers without validation.",
                Metadata = { ["InjectedHost"] = SpoofedHost, ["PerceivedHost"] = perceivedHost }
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
            Details = $"Application securely protected Host context. Perceived Host remained '{perceivedHost}'.",
            Metadata = { ["InjectedHost"] = SpoofedHost, ["PerceivedHost"] = perceivedHost ?? "" }
        };
    }
}
