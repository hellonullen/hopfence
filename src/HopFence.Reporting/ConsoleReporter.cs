using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Reporting;

/// <summary>
/// Renders diagnostic results with ANSI color highlights and formatted remediation summaries for the CLI.
/// </summary>
public sealed class ConsoleReporter : IHopFenceReporter
{
    public string FormatName => "console";

    // ANSI Escape Codes
    private const string Reset = "\u001b[0m";
    private const string Bold = "\u001b[1m";
    private const string Dim = "\u001b[2m";
    private const string Red = "\u001b[31m";
    private const string Green = "\u001b[32m";
    private const string Yellow = "\u001b[33m";
    private const string Blue = "\u001b[34m";
    private const string Magenta = "\u001b[35m";
    private const string Cyan = "\u001b[36m";
    private const string White = "\u001b[37m";

    public string RenderToString(DiagnosticReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine($"{Cyan}{Bold}  _    _             ______                    {Reset}");
        sb.AppendLine($"{Cyan}{Bold} | |  | |           |  ____|                   {Reset}");
        sb.AppendLine($"{Cyan}{Bold} | |__| | ___  _ __ | |__ ___ _ __   ___ ___   {Reset}");
        sb.AppendLine($"{Cyan}{Bold} |  __  |/ _ \\| '_ \\|  __/ _ \\ '_ \\ / __/ _ \\  {Reset}");
        sb.AppendLine($"{Cyan}{Bold} | |  | | (_) | |_) | | |  __/ | | | (_|  __/  {Reset}");
        sb.AppendLine($"{Cyan}{Bold} |_|  |_|\\___/| .__/|_|  \\___|_| |_|\\___\\___|  {Reset}");
        sb.AppendLine($"{Cyan}{Bold}              | |                              {Reset}");
        sb.AppendLine($"{Cyan}{Bold}              |_|    HopFence — Reverse Proxy Diagnostics  {Reset}");
        sb.AppendLine($"{Dim}  Created by NulleN (Kadircan Üredi) • https://kadircanuredi.com.tr{Reset}");
        sb.AppendLine();

        sb.AppendLine($"{Bold}Target URL:{Reset}       {report.TargetUrl}");
        sb.AppendLine($"{Bold}Framework:{Reset}        {report.Framework ?? "Unknown"}");
        sb.AppendLine($"{Bold}Baseline Scheme:{Reset}  {report.BaselineScheme ?? "N/A"}");
        sb.AppendLine($"{Bold}Resolved Client:{Reset}  {report.BaselineClientIp ?? "N/A"}");
        sb.AppendLine($"{Bold}Proxy Socket IP:{Reset}  {report.BaselineSocketPeerIp ?? "N/A"}");
        sb.AppendLine($"{Bold}Execution Time:{Reset}   {report.TotalDurationMs} ms");
        sb.AppendLine(new string('─', 80));

        // Test Table
        sb.AppendLine($"{"STATUS",-14} {"TEST ID",-18} {"NAME",-35} {"DURATION",8}");
        sb.AppendLine(new string('─', 80));

        foreach (var test in report.Results)
        {
            string statusBadge = test.Status switch
            {
                TestStatus.Pass => $"{Green}{Bold}[PASS]{Reset}        ",
                TestStatus.Fail => $"{Red}{Bold}[FAIL]{Reset}        ",
                TestStatus.Warn => $"{Yellow}{Bold}[WARN]{Reset}        ",
                TestStatus.Inconclusive => $"{Magenta}{Bold}[INCL]{Reset}        ",
                TestStatus.Skip => $"{Dim}[SKIP]{Reset}        ",
                _ => $"{White}[????]{Reset}        "
            };

            string testName = test.Name.Length > 34 ? test.Name.Substring(0, 31) + "..." : test.Name;
            sb.AppendLine($"{statusBadge} {test.TestId,-18} {testName,-35} {test.DurationMs,6}ms");

            if (test.Status == TestStatus.Fail || test.Status == TestStatus.Warn)
            {
                sb.AppendLine($"  {Dim}└─ {test.Details}{Reset}");
            }
        }

        sb.AppendLine(new string('─', 80));

        // Remediations block if any issues
        var issues = report.Results.Where(r => r.Status == TestStatus.Fail || r.Status == TestStatus.Warn).ToList();
        if (issues.Any())
        {
            sb.AppendLine();
            sb.AppendLine($"{Yellow}{Bold}⚠️  DIAGNOSTIC & SECURITY REMEDIATIONS:{Reset}");
            sb.AppendLine();

            foreach (var issue in issues)
            {
                string tagColor = issue.Status == TestStatus.Fail ? Red : Yellow;
                sb.AppendLine($"{tagColor}{Bold}● [{issue.TestId}] {issue.Name} ({issue.Severity} Severity){Reset}");
                sb.AppendLine($"  {Bold}Finding:{Reset} {issue.Details}");
                if (!string.IsNullOrWhiteSpace(issue.Remediation))
                {
                    sb.AppendLine($"  {Bold}Remediation Guide:{Reset}");
                    foreach (var line in issue.Remediation.Split('\n'))
                    {
                        sb.AppendLine($"    {line.TrimEnd()}");
                    }
                }
                sb.AppendLine();
            }
        }

        // Summary Line
        sb.AppendLine(new string('═', 80));
        sb.Append($"{Bold}SUMMARY:{Reset} ");
        sb.Append($"{Green}Pass: {report.TotalPassed}{Reset} | ");
        sb.Append(report.TotalFailed > 0 ? $"{Red}{Bold}Fail: {report.TotalFailed}{Reset} | " : $"Fail: 0 | ");
        sb.Append(report.TotalWarned > 0 ? $"{Yellow}Warn: {report.TotalWarned}{Reset} | " : $"Warn: 0 | ");
        sb.Append($"{Magenta}Inconclusive: {report.TotalInconclusive}{Reset} | ");
        sb.Append($"{Dim}Skipped: {report.TotalSkipped}{Reset}");
        sb.AppendLine();
        sb.AppendLine(new string('═', 80));

        if (report.IsSuccess)
        {
            sb.AppendLine($"{Green}{Bold}✓ All proxy diagnostic checks passed successfully!{Reset}");
        }
        else
        {
            sb.AppendLine($"{Red}{Bold}✗ Security vulnerabilities or configuration errors were detected!{Reset}");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    public async Task WriteToFileAsync(DiagnosticReport report, string filePath)
    {
        string text = RenderToString(report);
        // Strip ANSI codes if writing to plain file
        string stripped = System.Text.RegularExpressions.Regex.Replace(text, @"\u001b\[[0-9;]*m", "");
        await File.WriteAllTextAsync(filePath, stripped);
    }
}
