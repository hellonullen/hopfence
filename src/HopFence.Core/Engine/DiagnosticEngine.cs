using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Http;
using HopFence.Core.Models;
using HopFence.Core.Tests;

namespace HopFence.Core.Engine;

/// <summary>
/// Core execution engine orchestrating HopFence diagnostic rules and generating final reports.
/// </summary>
public sealed class DiagnosticEngine
{
    private readonly List<IDiagnosticTest> _tests;

    public DiagnosticEngine(IEnumerable<IDiagnosticTest>? customTests = null)
    {
        if (customTests != null)
        {
            _tests = new List<IDiagnosticTest>(customTests);
        }
        else
        {
            // Default battery of tests (A through G)
            _tests = new List<IDiagnosticTest>
            {
                new HttpsDetectionTest(),
                new ClientIpDetectionTest(),
                new ForwardedForSpoofingTest(),
                new ForwardedProtoSpoofingTest(),
                new ForwardedHostSpoofingTest(),
                new RedirectTest(),
                new PathBaseTest()
            };
        }
    }

    /// <summary>
    /// Executes the complete diagnostic suite against the target.
    /// </summary>
    public async Task<DiagnosticReport> RunDiagnosticsAsync(
        HopFenceConfig config,
        IProbeClient? client = null,
        Action<string>? logVerbose = null,
        CancellationToken cancellationToken = default)
    {
        var totalSw = Stopwatch.StartNew();
        var report = new DiagnosticReport
        {
            TargetUrl = config.Target.BaseUrl,
            Timestamp = DateTimeOffset.UtcNow
        };

        bool disposeClient = false;
        IProbeClient probeClient = client!;
        if (probeClient == null)
        {
            probeClient = new ProbeClient(config.Target);
            disposeClient = true;
        }

        try
        {
            var context = new DiagnosticContext(config, probeClient, logVerbose);

            // 1. Establish baseline probe
            logVerbose?.Invoke($"Probing baseline target: {probeClient.TargetProbeUri}");
            var baselineResult = await probeClient.ExecuteProbeAsync(cancellationToken: cancellationToken);

            if (baselineResult.IsSuccessStatusCode && baselineResult.ProbeData != null)
            {
                context.BaselineProbe = baselineResult.ProbeData;
                report.Framework = baselineResult.ProbeData.Framework;
                report.SchemaVersion = baselineResult.ProbeData.SchemaVersion;
                report.BaselineClientIp = baselineResult.ProbeData.Request?.ClientIp;
                report.BaselineSocketPeerIp = baselineResult.ProbeData.Request?.SocketPeerIp;
                report.BaselineScheme = baselineResult.ProbeData.Request?.Scheme;
                report.BaselineHost = baselineResult.ProbeData.Request?.Host;
                logVerbose?.Invoke($"Baseline established. Framework: '{report.Framework}', Client IP: '{report.BaselineClientIp}'");
            }
            else
            {
                logVerbose?.Invoke($"Baseline probe warning: HTTP {(int)baselineResult.StatusCode} - {baselineResult.ErrorMessage}");
            }

            // 2. Execute test battery
            foreach (var test in _tests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!test.IsEnabled(config))
                {
                    report.Results.Add(new TestResult
                    {
                        TestId = test.Id,
                        Name = test.Name,
                        Description = test.Description,
                        Status = TestStatus.Skip,
                        Severity = TestSeverity.Info,
                        DurationMs = 0,
                        Details = "Test disabled by configuration."
                    });
                    continue;
                }

                logVerbose?.Invoke($"Executing {test.Id} ({test.Name})...");
                try
                {
                    var result = await test.ExecuteAsync(context, cancellationToken);
                    report.Results.Add(result);
                    logVerbose?.Invoke($"Test {test.Id} completed with status: {result.Status}");
                }
                catch (Exception ex)
                {
                    report.Results.Add(new TestResult
                    {
                        TestId = test.Id,
                        Name = test.Name,
                        Description = test.Description,
                        Status = TestStatus.Inconclusive,
                        Severity = TestSeverity.High,
                        DurationMs = 0,
                        Details = $"Unexpected exception during test execution: {ex.Message}"
                    });
                }
            }
        }
        finally
        {
            if (disposeClient)
            {
                probeClient.Dispose();
            }
            totalSw.Stop();
            report.TotalDurationMs = totalSw.ElapsedMilliseconds;
        }

        return report;
    }
}
