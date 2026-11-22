using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test D: Tests whether an attacker can downgrade connection security context via X-Forwarded-Proto header.
/// </summary>
public sealed class ForwardedProtoSpoofingTest : IDiagnosticTest
{
    public const string SpoofedProto = "http";

    public string Id => "HOP-SPOOF-PROTO-001";
    public string Name => "X-Forwarded-Proto Downgrade Protection";
    public string Description => "Injects an untrusted 'X-Forwarded-Proto: http' header to verify protocol downgrade protection.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.ForwardedProtoSpoofing;

    public async Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        // If target scheme or expected scheme is not https, this test is inconclusive/skip
        bool isHttpsTarget = context.Client.TargetBaseUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(context.Config.Expect.Scheme, "https", StringComparison.OrdinalIgnoreCase);

        if (!isHttpsTarget)
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Skip,
                Severity = TestSeverity.Low,
                DurationMs = 0,
                Details = "Test skipped because target is not configured for HTTPS."
            };
        }

        var customHeaders = new Dictionary<string, string>
        {
            ["X-Forwarded-Proto"] = SpoofedProto
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

        string? perceivedScheme = probeResult.ProbeData.Request?.Scheme;

        if (string.Equals(perceivedScheme, SpoofedProto, StringComparison.OrdinalIgnoreCase))
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Fail,
                Severity = TestSeverity.High,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"VULNERABILITY: Application downgraded connection scheme to '{perceivedScheme}' over an HTTPS connection based on client-supplied X-Forwarded-Proto! " +
                          "This can trigger infinite redirect loops, drop Secure cookie attributes, or permit SSL stripping.",
                Remediation = "In Nginx: Ensure 'proxy_set_header X-Forwarded-Proto $scheme;' is configured in the server block so Nginx overrides any client-sent header.",
                Metadata = { ["InjectedProto"] = SpoofedProto, ["PerceivedScheme"] = perceivedScheme ?? "" }
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
            Details = $"Application securely maintained scheme as '{perceivedScheme}' and ignored spoofed '{SpoofedProto}'.",
            Metadata = { ["InjectedProto"] = SpoofedProto, ["PerceivedScheme"] = perceivedScheme ?? "" }
        };
    }
}
