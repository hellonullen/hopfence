using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Reporting;

/// <summary>
/// Serializes diagnostic results to standardized JSON format.
/// </summary>
public sealed class JsonReporter : IHopFenceReporter
{
    public string FormatName => "json";

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string RenderToString(DiagnosticReport report)
    {
        return JsonSerializer.Serialize(report, _options);
    }

    public async Task WriteToFileAsync(DiagnosticReport report, string filePath)
    {
        string json = RenderToString(report);
        await File.WriteAllTextAsync(filePath, json);
    }
}
