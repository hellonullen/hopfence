using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test C: Validates that the application and reverse proxy reject client-injected X-Forwarded-For headers.
/// </summary>
public sealed class ForwardedForSpoofingTest : IDiagnosticTest
{
    public const string SpoofedIp = "198.51.100.77"; // IANA TEST-NET-2 dummy IP

    public string Id => "HOP-SPOOF-XFF-001";
    public string Name => "X-Forwarded-For Spoofing Protection";
    public string Description => "Injects an untrusted X-Forwarded-For header to verify whether the reverse proxy or application falls victim to IP spoofing.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.ForwardedForSpoofing;

    public async Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var customHeaders = new Dictionary<string, string>
        {
            ["X-Forwarded-For"] = SpoofedIp
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
                Severity = TestSeverity.High,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"Probe request with spoofed header failed with HTTP {(int)probeResult.StatusCode}: {probeResult.ErrorMessage ?? probeResult.RawBody}"
            };
        }

        string? resolvedClientIp = probeResult.ProbeData.Request?.ClientIp;

        if (string.Equals(resolvedClientIp, SpoofedIp, StringComparison.OrdinalIgnoreCase))
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Fail,
                Severity = TestSeverity.Critical,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"CRITICAL VULNERABILITY: Application accepted the attacker-injected IP '{SpoofedIp}' as its genuine client IP! " +
                          "An attacker can bypass IP allowlists, IP-based rate limiting, geo-restrictions, and spoof security audit logs.",
                Remediation = "1. In Nginx: Never blindly pass client headers. Use 'proxy_set_header X-Forwarded-For $remote_addr;' or verify your proxy chain.\n" +
                              "2. In ASP.NET Core: Configure ForwardedHeadersOptions.KnownProxies or KnownNetworks explicitly. Never clear them to accept arbitrary proxy hops.\n" +
                              "3. In Express: Never set 'trust proxy' to 'true' without an IP or CIDR filter. Use 'app.set(\"trust proxy\", [\"loopback\", \"linklocal\", \"uniquelocal\"])' or specify exact proxy subnet.",
                Metadata = { ["InjectedIp"] = SpoofedIp, ["ResolvedIp"] = resolvedClientIp ?? "" }
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
            Details = $"Application successfully rejected injected IP '{SpoofedIp}'. Real resolved client IP remained '{resolvedClientIp}'.",
            Metadata = { ["InjectedIp"] = SpoofedIp, ["ResolvedIp"] = resolvedClientIp ?? "" }
        };
    }
}
