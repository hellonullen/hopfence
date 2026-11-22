using System.IO;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Reporting;

/// <summary>
/// Interface for rendering HopFence diagnostic reports in various output formats.
/// </summary>
public interface IHopFenceReporter
{
    /// <summary>Format identifier name (e.g. "console", "json", "html", "junit").</summary>
    string FormatName { get; }

    /// <summary>Renders the report to a string.</summary>
    string RenderToString(DiagnosticReport report);

    /// <summary>Writes the rendered report to a file path.</summary>
    Task WriteToFileAsync(DiagnosticReport report, string filePath);
}
