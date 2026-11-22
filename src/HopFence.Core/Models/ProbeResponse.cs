using System.Text.Json.Serialization;

namespace HopFence.Core.Models;

/// <summary>
/// Standardized JSON schema v1 returned by HopFence probe endpoints across ASP.NET Core and Express.
/// </summary>
public sealed class ProbeResponse
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("framework")]
    public string Framework { get; set; } = "unknown";

    [JsonPropertyName("request")]
    public ProbeRequestData Request { get; set; } = new();
}

/// <summary>
/// Detailed HTTP request state captured by the target application behind the reverse proxy.
/// </summary>
public sealed class ProbeRequestData
{
    [JsonPropertyName("clientIp")]
    public string? ClientIp { get; set; }

    [JsonPropertyName("socketPeerIp")]
    public string? SocketPeerIp { get; set; }

    [JsonPropertyName("scheme")]
    public string? Scheme { get; set; }

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("pathBase")]
    public string? PathBase { get; set; }

    [JsonPropertyName("forwardedHeaders")]
    public ForwardedHeadersInfo ForwardedHeaders { get; set; } = new();
}

/// <summary>
/// Presence and raw indicators of reverse proxy forwarded headers.
/// </summary>
public sealed class ForwardedHeadersInfo
{
    [JsonPropertyName("xForwardedForPresent")]
    public bool XForwardedForPresent { get; set; }

    [JsonPropertyName("xForwardedProtoPresent")]
    public bool XForwardedProtoPresent { get; set; }

    [JsonPropertyName("xForwardedHostPresent")]
    public bool XForwardedHostPresent { get; set; }

    [JsonPropertyName("rawXForwardedFor")]
    public string? RawXForwardedFor { get; set; }

    [JsonPropertyName("rawXForwardedProto")]
    public string? RawXForwardedProto { get; set; }

    [JsonPropertyName("rawXForwardedHost")]
    public string? RawXForwardedHost { get; set; }
}
