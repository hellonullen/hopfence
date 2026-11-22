using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Reporting;

/// <summary>
/// Renders diagnostic reports to JUnit XML standard for CI/CD test visualizers.
/// </summary>
public sealed class JUnitReporter : IHopFenceReporter
{
    public string FormatName => "junit";

    public string RenderToString(DiagnosticReport report)
    {
        var sb = new StringBuilder();
        double totalSeconds = report.TotalDurationMs / 1000.0;

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
            "<testsuites name=\"HopFence Diagnostics\" tests=\"{0}\" failures=\"{1}\" errors=\"0\" skipped=\"{2}\" time=\"{3:F3}\">",
            report.Results.Count, report.TotalFailed, report.TotalSkipped, totalSeconds));

        sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
            "  <testsuite name=\"HopFence.ReverseProxyChecks\" tests=\"{0}\" failures=\"{1}\" errors=\"0\" skipped=\"{2}\" time=\"{3:F3}\" timestamp=\"{4:yyyy-MM-ddTHH:mm:ssZ}\">",
            report.Results.Count, report.TotalFailed, report.TotalSkipped, totalSeconds, report.Timestamp));

        foreach (var test in report.Results)
        {
            double testSeconds = test.DurationMs / 1000.0;
            string safeName = SecurityElement.Escape($"[{test.TestId}] {test.Name}");
            string safeClass = SecurityElement.Escape($"HopFence.Diagnostics.{test.TestId}");

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "    <testcase classname=\"{0}\" name=\"{1}\" time=\"{2:F3}\">",
                safeClass, safeName, testSeconds));

            if (test.Status == TestStatus.Fail)
            {
                string safeDetails = SecurityElement.Escape($"{test.Details}\n\nRemediation:\n{test.Remediation}");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "      <failure message=\"{0}\" type=\"{1}\">{2}</failure>",
                    SecurityElement.Escape(test.Details), test.Severity, safeDetails));
            }
            else if (test.Status == TestStatus.Skip)
            {
                sb.AppendLine("      <skipped />");
            }

            sb.AppendLine("    </testcase>");
        }

        sb.AppendLine("  </testsuite>");
        sb.AppendLine("</testsuites>");

        return sb.ToString();
    }

    public async Task WriteToFileAsync(DiagnosticReport report, string filePath)
    {
        string xml = RenderToString(report);
        await File.WriteAllTextAsync(filePath, xml, Encoding.UTF8);
    }
}
