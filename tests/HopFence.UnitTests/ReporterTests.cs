using System;
using System.Collections.Generic;
using HopFence.Core.Models;
using HopFence.Reporting;
using Xunit;

namespace HopFence.UnitTests;

public class ReporterTests
{
    private DiagnosticReport CreateSampleReport()
    {
        return new DiagnosticReport
        {
            TargetUrl = "https://staging.example.com",
            Framework = "aspnetcore",
            BaselineClientIp = "203.0.113.5",
            BaselineSocketPeerIp = "172.20.0.2",
            BaselineScheme = "https",
            BaselineHost = "staging.example.com",
            TotalDurationMs = 245,
            Results = new List<TestResult>
            {
                new()
                {
                    TestId = "HOP-HTTPS-001",
                    Name = "HTTPS Protocol Detection",
                    Description = "Verifies HTTPS scheme detection.",
                    Status = TestStatus.Pass,
                    Severity = TestSeverity.Info,
                    DurationMs = 30,
                    Details = "Backend recognized scheme as 'https'."
                },
                new()
                {
                    TestId = "HOP-SPOOF-XFF-001",
                    Name = "X-Forwarded-For Spoofing Protection",
                    Description = "Injects spoofed IP.",
                    Status = TestStatus.Fail,
                    Severity = TestSeverity.Critical,
                    DurationMs = 45,
                    Details = "CRITICAL VULNERABILITY: Application accepted injected IP '198.51.100.77'.",
                    Remediation = "Configure KnownProxies explicitly in ASP.NET Core."
                }
            }
        };
    }

    [Fact]
    public void ConsoleReporter_RendersSummaryAndBadges()
    {
        var reporter = new ConsoleReporter();
        var report = CreateSampleReport();

        string output = reporter.RenderToString(report);

        Assert.Contains("HopFence", output);
        Assert.Contains("HOP-HTTPS-001", output);
        Assert.Contains("HOP-SPOOF-XFF-001", output);
        Assert.Contains("[PASS]", output);
        Assert.Contains("[FAIL]", output);
        Assert.Contains("CRITICAL VULNERABILITY", output);
        Assert.Contains("Kadircan", output);
    }

    [Fact]
    public void JsonReporter_ProducesValidJsonWithProperties()
    {
        var reporter = new JsonReporter();
        var report = CreateSampleReport();

        string json = reporter.RenderToString(report);

        Assert.Contains("\"targetUrl\": \"https://staging.example.com\"", json);
        Assert.Contains("\"framework\": \"aspnetcore\"", json);
        Assert.Contains("\"isSuccess\": false", json);
        Assert.Contains("\"totalFailed\": 1", json);
    }

    [Fact]
    public void HtmlReporter_RendersValidHtmlWithDarkThemeAndCredits()
    {
        var reporter = new HtmlReporter();
        var report = CreateSampleReport();

        string html = reporter.RenderToString(report);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("HopFence Diagnostic Report", html);
        Assert.Contains("kadircanuredi.com.tr", html);
        Assert.Contains("hellonullen@gmail.com", html);
        Assert.Contains("ACTION REQUIRED", html);
        Assert.Contains("HOP-SPOOF-XFF-001", html);
    }

    [Fact]
    public void JUnitReporter_RendersValidXmlWithFailureElement()
    {
        var reporter = new JUnitReporter();
        var report = CreateSampleReport();

        string xml = reporter.RenderToString(report);

        Assert.Contains("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml);
        Assert.Contains("<testsuites", xml);
        Assert.Contains("failures=\"1\"", xml);
        Assert.Contains("<failure message=", xml);
        Assert.Contains("</testsuites>", xml);
    }
}
