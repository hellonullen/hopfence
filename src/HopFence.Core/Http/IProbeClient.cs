using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Core.Http;

/// <summary>
/// Result of an HTTP probe execution against the target application.
/// </summary>
public sealed class ProbeExecutionResult
{
    public HttpStatusCode StatusCode { get; set; }
    public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
    public ProbeResponse? ProbeData { get; set; }
    public string RawBody { get; set; } = string.Empty;
    public HttpResponseMessage? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Interface for issuing authenticated diagnostic requests to the target application.
/// </summary>
public interface IProbeClient : IDisposable
{
    /// <summary>
    /// Executes a probe request to the configured probe endpoint with optional custom headers.
    /// </summary>
    Task<ProbeExecutionResult> ExecuteProbeAsync(
        IDictionary<string, string>? customHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an HTTP request to any specified URL or path on the target (useful for redirect testing).
    /// </summary>
    Task<HttpResponseMessage> SendRawAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the resolved target URI.
    /// </summary>
    Uri TargetBaseUri { get; }

    /// <summary>
    /// Returns the target probe URI.
    /// </summary>
    Uri TargetProbeUri { get; }
}
