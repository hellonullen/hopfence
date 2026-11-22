using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test B: Verifies that the backend application resolves the real client IP address correctly.
/// </summary>
public sealed class ClientIpDetectionTest : IDiagnosticTest
{
    public string Id => "HOP-IP-001";
    public string Name => "Client IP Detection";
    public string Description => "Checks whether the backend receives and calculates the true client IP address.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.ClientIpDetection;

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
                Details = $"Probe request failed: {probeResult.ErrorMessage ?? probeResult.RawBody}"
            };
        }

        string? clientIp = probeResult.ProbeData.Request?.ClientIp;
        string? socketPeerIp = probeResult.ProbeData.Request?.SocketPeerIp;
        string? expectedIp = context.Config.Expect.ClientIp;

        if (string.IsNullOrWhiteSpace(clientIp))
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Fail,
                Severity = TestSeverity.High,
                DurationMs = sw.ElapsedMilliseconds,
                Details = "Backend application reported an empty or null client IP address.",
                Remediation = "Verify that ForwardedHeaders middleware or Express trust proxy is enabled."
            };
        }

        // Case 1: User specified expectedClientIp in hopfence.yml
        if (!string.IsNullOrWhiteSpace(expectedIp))
        {
            if (string.Equals(clientIp, expectedIp, StringComparison.OrdinalIgnoreCase))
            {
                return new TestResult
                {
                    TestId = Id,
                    Name = Name,
                    Description = Description,
                    Status = TestStatus.Pass,
                    Severity = TestSeverity.Info,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = $"Client IP matched expected address '{expectedIp}'. (Proxy socket IP: {socketPeerIp ?? "N/A"})",
                    Metadata = { ["ClientIp"] = clientIp, ["SocketPeerIp"] = socketPeerIp ?? "", ["ExpectedIp"] = expectedIp }
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
                Details = $"Calculated client IP '{clientIp}' does not match expected IP '{expectedIp}'. (Socket peer IP: {socketPeerIp ?? "N/A"})",
                Remediation = "Check proxy forwarding headers (X-Forwarded-For) and verify proxy hop count / known network CIDRs."
            };
        }

        // Case 2: expectedClientIp is null (Heuristic evaluation)
        bool isLoopback = IPAddress.TryParse(clientIp, out var parsedIp) && IPAddress.IsLoopback(parsedIp);
        bool sameAsSocket = string.Equals(clientIp, socketPeerIp, StringComparison.OrdinalIgnoreCase);

        if (isLoopback && !string.IsNullOrWhiteSpace(socketPeerIp) && !sameAsSocket)
        {
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Warn,
                Severity = TestSeverity.Medium,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"Application resolved loopback address '{clientIp}' while socket IP is '{socketPeerIp}'.",
                Remediation = "If running behind an external proxy, ensure X-Forwarded-For is forwarded and trusted by the backend.",
                Metadata = { ["ClientIp"] = clientIp, ["SocketPeerIp"] = socketPeerIp ?? "" }
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
            Details = $"Client IP successfully identified as '{clientIp}' (Proxy connection socket: {socketPeerIp ?? "direct"}). " +
                      "Note: For strict assertion against your public IP, configure 'expect.clientIp' in hopfence.yml.",
            Metadata = { ["ClientIp"] = clientIp, ["SocketPeerIp"] = socketPeerIp ?? "" }
        };
    }
}
