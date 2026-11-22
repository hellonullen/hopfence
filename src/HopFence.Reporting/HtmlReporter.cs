using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Reporting;

/// <summary>
/// Renders an interactive, modern, standalone HTML report with dark mode and zero external dependencies.
/// </summary>
public sealed class HtmlReporter : IHopFenceReporter
{
    public string FormatName => "html";

    public string RenderToString(DiagnosticReport report)
    {
        var sb = new StringBuilder();

        string overallColor = report.IsSuccess ? "#238636" : "#da3633";
        string overallBadge = report.IsSuccess ? "SECURE & COMPLIANT" : "ACTION REQUIRED";
        string verdictText = report.IsSuccess
            ? "All reverse proxy diagnostic and security checks passed successfully."
            : $"{report.TotalFailed} critical issue(s) or misconfiguration(s) were identified.";

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>HopFence Diagnostic Report — {WebUtility.HtmlEncode(report.TargetUrl)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
    :root {
      --bg: #0d1117;
      --card-bg: #161b22;
      --border: #30363d;
      --text: #c9d1d9;
      --text-bright: #f0f6fc;
      --text-muted: #8b949e;
      --pass: #2ea043;
      --fail: #f85149;
      --warn: #d29922;
      --info: #58a6ff;
      --skip: #6e7681;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background: var(--bg);
      color: var(--text);
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      line-height: 1.6;
      padding: 24px;
    }
    .container { max-width: 1100px; margin: 0 auto; }
    header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 20px;
      border-bottom: 1px solid var(--border);
      margin-bottom: 24px;
    }
    .logo-box h1 {
      font-size: 26px;
      color: var(--text-bright);
      letter-spacing: -0.5px;
    }
    .logo-box p {
      color: var(--text-muted);
      font-size: 14px;
    }
    .meta-box { text-align: right; font-size: 13px; color: var(--text-muted); }
    .verdict-banner {
      background: var(--card-bg);
      border-left: 6px solid;
      border-radius: 6px;
      padding: 20px;
      margin-bottom: 24px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .verdict-info h2 { color: var(--text-bright); font-size: 18px; margin-bottom: 4px; }
    .verdict-info p { color: var(--text-muted); font-size: 14px; }
    .badge {
      display: inline-block;
      padding: 6px 14px;
      border-radius: 20px;
      font-weight: 700;
      font-size: 12px;
      letter-spacing: 0.5px;
      text-transform: uppercase;
    }
    .stat-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 16px;
      margin-bottom: 28px;
    }
    .stat-card {
      background: var(--card-bg);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 18px;
      text-align: center;
    }
    .stat-card .val {
      font-size: 32px;
      font-weight: 700;
      color: var(--text-bright);
      margin-bottom: 2px;
    }
    .stat-card .lbl { font-size: 13px; color: var(--text-muted); text-transform: uppercase; font-weight: 600; }
    .test-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
      margin-bottom: 36px;
    }
    .test-item {
      background: var(--card-bg);
      border: 1px solid var(--border);
      border-radius: 6px;
      overflow: hidden;
    }
    .test-header {
      padding: 14px 18px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      cursor: pointer;
      user-select: none;
    }
    .test-title {
      display: flex;
      align-items: center;
      gap: 14px;
    }
    .test-title h3 {
      font-size: 15px;
      color: var(--text-bright);
      font-weight: 600;
    }
    .test-id {
      font-family: monospace;
      font-size: 12px;
      color: var(--text-muted);
      background: #21262d;
      padding: 2px 8px;
      border-radius: 4px;
    }
    .status-pill {
      font-size: 11px;
      font-weight: 700;
      padding: 3px 10px;
      border-radius: 12px;
      text-transform: uppercase;
    }
    .pill-pass { background: rgba(46,160,67,0.2); color: var(--pass); border: 1px solid var(--pass); }
    .pill-fail { background: rgba(248,81,73,0.2); color: var(--fail); border: 1px solid var(--fail); }
    .pill-warn { background: rgba(210,153,34,0.2); color: var(--warn); border: 1px solid var(--warn); }
    .pill-skip { background: rgba(110,118,129,0.2); color: var(--skip); border: 1px solid var(--skip); }
    .pill-inconclusive { background: rgba(88,166,255,0.2); color: var(--info); border: 1px solid var(--info); }
    .test-body {
      padding: 16px 18px;
      border-top: 1px solid var(--border);
      background: #0d1117;
      font-size: 14px;
    }
    .details-row { margin-bottom: 12px; }
    .remediation-box {
      background: #161b22;
      border-left: 4px solid var(--fail);
      padding: 12px 16px;
      margin-top: 10px;
      border-radius: 4px;
    }
    .remediation-box h4 { color: #f85149; font-size: 13px; margin-bottom: 6px; }
    .remediation-box pre {
      font-family: Consolas, monospace;
      background: #0d1117;
      padding: 10px;
      border-radius: 4px;
      overflow-x: auto;
      color: #79c0ff;
      font-size: 13px;
    }
    footer {
      border-top: 1px solid var(--border);
      padding-top: 20px;
      text-align: center;
      color: var(--text-muted);
      font-size: 13px;
    }
    footer a { color: var(--info); text-decoration: none; }
    footer a:hover { text-decoration: underline; }
");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");

        // Header
        sb.AppendLine("    <header>");
        sb.AppendLine("      <div class=\"logo-box\">");
        sb.AppendLine("        <h1>HopFence</h1>");
        sb.AppendLine("        <p>Reverse Proxy Diagnostics & Security Testing Tool</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"meta-box\">");
        sb.AppendLine($"        <div><strong>Target:</strong> {WebUtility.HtmlEncode(report.TargetUrl)}</div>");
        sb.AppendLine($"        <div><strong>Date:</strong> {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</div>");
        sb.AppendLine($"        <div><strong>Framework:</strong> {WebUtility.HtmlEncode(report.Framework ?? "Unknown")}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </header>");

        // Verdict Banner
        sb.AppendLine($"    <div class=\"verdict-banner\" style=\"border-color: {overallColor};\">");
        sb.AppendLine("      <div class=\"verdict-info\">");
        sb.AppendLine($"        <h2>{verdictText}</h2>");
        sb.AppendLine($"        <p>Target probe: {WebUtility.HtmlEncode(report.TargetUrl)} | Duration: {report.TotalDurationMs} ms</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <div class=\"badge\" style=\"background: {overallColor}; color: #ffffff;\">{overallBadge}</div>");
        sb.AppendLine("    </div>");

        // Stat Grid
        sb.AppendLine("    <div class=\"stat-grid\">");
        sb.AppendLine($"      <div class=\"stat-card\"><div class=\"val\">{report.Results.Count}</div><div class=\"lbl\">Total Tests</div></div>");
        sb.AppendLine($"      <div class=\"stat-card\"><div class=\"val\" style=\"color: var(--pass);\">{report.TotalPassed}</div><div class=\"lbl\">Passed</div></div>");
        sb.AppendLine($"      <div class=\"stat-card\"><div class=\"val\" style=\"color: var(--fail);\">{report.TotalFailed}</div><div class=\"lbl\">Failed</div></div>");
        sb.AppendLine($"      <div class=\"stat-card\"><div class=\"val\" style=\"color: var(--warn);\">{report.TotalWarned}</div><div class=\"lbl\">Warnings</div></div>");
        sb.AppendLine($"      <div class=\"stat-card\"><div class=\"val\" style=\"color: var(--info);\">{report.TotalDurationMs}ms</div><div class=\"lbl\">Total Time</div></div>");
        sb.AppendLine("    </div>");

        // Test List
        sb.AppendLine("    <div class=\"test-list\">");

        foreach (var test in report.Results)
        {
            string statusPillClass = test.Status switch
            {
                TestStatus.Pass => "pill-pass",
                TestStatus.Fail => "pill-fail",
                TestStatus.Warn => "pill-warn",
                TestStatus.Skip => "pill-skip",
                _ => "pill-inconclusive"
            };

            sb.AppendLine("      <div class=\"test-item\">");
            sb.AppendLine("        <div class=\"test-header\">");
            sb.AppendLine("          <div class=\"test-title\">");
            sb.AppendLine($"            <span class=\"status-pill {statusPillClass}\">{test.Status}</span>");
            sb.AppendLine($"            <span class=\"test-id\">{WebUtility.HtmlEncode(test.TestId)}</span>");
            sb.AppendLine($"            <h3>{WebUtility.HtmlEncode(test.Name)}</h3>");
            sb.AppendLine("          </div>");
            sb.AppendLine($"          <div style=\"font-size: 13px; color: var(--text-muted);\">{test.DurationMs} ms</div>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"test-body\">");
            sb.AppendLine($"          <div class=\"details-row\"><strong>Description:</strong> {WebUtility.HtmlEncode(test.Description)}</div>");
            sb.AppendLine($"          <div class=\"details-row\"><strong>Finding:</strong> {WebUtility.HtmlEncode(test.Details)}</div>");

            if (!string.IsNullOrWhiteSpace(test.Remediation))
            {
                sb.AppendLine("          <div class=\"remediation-box\">");
                sb.AppendLine("            <h4>Recommended Security Remediation</h4>");
                sb.AppendLine($"            <pre>{WebUtility.HtmlEncode(test.Remediation)}</pre>");
                sb.AppendLine("          </div>");
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
        }

        sb.AppendLine("    </div>");

        // Footer
        sb.AppendLine("    <footer>");
        sb.AppendLine("      <p>Created with pride by <strong>NulleN - Kadircan Üredi</strong> &bull; <a href=\"https://kadircanuredi.com.tr\" target=\"_blank\">kadircanuredi.com.tr</a> &bull; <a href=\"mailto:hellonullen@gmail.com\">hellonullen@gmail.com</a></p>");
        sb.AppendLine("      <p style=\"margin-top: 6px; font-size: 12px;\">HopFence is free & open-source under the MIT License.</p>");
        sb.AppendLine("    </footer>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public async Task WriteToFileAsync(DiagnosticReport report, string filePath)
    {
        string html = RenderToString(report);
        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
    }
}
