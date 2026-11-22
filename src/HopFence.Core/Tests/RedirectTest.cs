using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Models;

namespace HopFence.Core.Tests;

/// <summary>
/// Test F: Validates HTTP to HTTPS redirection, loops, hop counts, and host boundary preservation.
/// </summary>
public sealed class RedirectTest : IDiagnosticTest
{
    public string Id => "HOP-REDIR-001";
    public string Name => "Redirect & Scheme Enforcement";
    public string Description => "Tests HTTP to HTTPS redirection, redirect loops, max hops, and host boundary preservation.";

    public bool IsEnabled(HopFenceConfig config) => config.Tests.Redirect;

    public async Task<TestResult> ExecuteAsync(DiagnosticContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var targetBaseUri = context.Client.TargetBaseUri;

        // Build the HTTP variant to test redirect from plain HTTP
        var builder = new UriBuilder(targetBaseUri)
        {
            Scheme = "http",
            Port = targetBaseUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && targetBaseUri.Port == 443 ? 80 : targetBaseUri.Port
        };
        var currentUri = builder.Uri;

        int maxHops = context.Config.Target.MaxRedirects > 0 ? context.Config.Target.MaxRedirects : 5;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int hopCount = 0;
        bool redirectedToHttps = false;
        string initialHost = targetBaseUri.Host;

        try
        {
            while (hopCount < maxHops)
            {
                if (!visited.Add(currentUri.ToString()))
                {
                    sw.Stop();
                    return new TestResult
                    {
                        TestId = Id,
                        Name = Name,
                        Description = Description,
                        Status = TestStatus.Fail,
                        Severity = TestSeverity.High,
                        DurationMs = sw.ElapsedMilliseconds,
                        Details = $"Redirect loop detected at '{currentUri}' after {hopCount} hops.",
                        Remediation = "Check reverse proxy or application HTTPS redirection rules to prevent circular redirects."
                    };
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.UserAgent.ParseAdd("HopFence-Diagnostics/1.0 (+https://kadircanuredi.com.tr)");

                using var response = await context.Client.SendRawAsync(request, cancellationToken);

                int statusCode = (int)response.StatusCode;
                bool isRedirect = statusCode is 301 or 302 or 307 or 308;

                if (!isRedirect)
                {
                    // Terminal response reached
                    break;
                }

                var location = response.Headers.Location;
                if (location == null)
                {
                    sw.Stop();
                    return new TestResult
                    {
                        TestId = Id,
                        Name = Name,
                        Description = Description,
                        Status = TestStatus.Fail,
                        Severity = TestSeverity.Medium,
                        DurationMs = sw.ElapsedMilliseconds,
                        Details = $"HTTP {statusCode} redirect response from '{currentUri}' did not include a Location header.",
                        Remediation = "Ensure the web server provides a valid Location header on redirect responses."
                    };
                }

                // Resolve relative locations against current URI
                var nextUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);

                // Check host boundary (avoid Open Redirect)
                if (!string.Equals(nextUri.Host, initialHost, StringComparison.OrdinalIgnoreCase))
                {
                    sw.Stop();
                    return new TestResult
                    {
                        TestId = Id,
                        Name = Name,
                        Description = Description,
                        Status = TestStatus.Warn,
                        Severity = TestSeverity.High,
                        DurationMs = sw.ElapsedMilliseconds,
                        Details = $"Redirect transitioned from expected host '{initialHost}' to foreign host '{nextUri.Host}' ({nextUri}).",
                        Remediation = "Verify that reverse proxy redirects do not leak traffic to external domains."
                    };
                }

                if (nextUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    redirectedToHttps = true;
                }

                currentUri = nextUri;
                hopCount++;
            }

            sw.Stop();

            if (hopCount >= maxHops)
            {
                return new TestResult
                {
                    TestId = Id,
                    Name = Name,
                    Description = Description,
                    Status = TestStatus.Fail,
                    Severity = TestSeverity.Medium,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = $"Exceeded maximum allowed redirect hops ({maxHops}). Last URI was '{currentUri}'.",
                    Remediation = "Reduce chained redirects in proxy configuration."
                };
            }

            if (targetBaseUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !redirectedToHttps && hopCount == 0)
            {
                // Target is already HTTPS, and probing HTTP was not redirected or HTTP port was directly answering
                return new TestResult
                {
                    TestId = Id,
                    Name = Name,
                    Description = Description,
                    Status = TestStatus.Pass,
                    Severity = TestSeverity.Info,
                    DurationMs = sw.ElapsedMilliseconds,
                    Details = "Direct HTTPS target tested. No redirect loops detected.",
                    Metadata = { ["HopCount"] = hopCount.ToString(), ["FinalUri"] = currentUri.ToString() }
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
                Details = $"Redirect checks passed successfully across {hopCount} hop(s). Terminated cleanly at '{currentUri}'.",
                Metadata = { ["HopCount"] = hopCount.ToString(), ["RedirectedToHttps"] = redirectedToHttps.ToString() }
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            // In many test environments or local servers, HTTP port (80) might not be open if only 443/5000 is running
            return new TestResult
            {
                TestId = Id,
                Name = Name,
                Description = Description,
                Status = TestStatus.Inconclusive,
                Severity = TestSeverity.Low,
                DurationMs = sw.ElapsedMilliseconds,
                Details = $"Could not complete HTTP redirect check: {ex.Message}. The HTTP endpoint may be disabled or blocked.",
                Remediation = "Verify whether port 80 / HTTP is intended to be open and redirecting to HTTPS."
            };
        }
    }
}
