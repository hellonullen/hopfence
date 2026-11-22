using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HopFence.Core.Models;

namespace HopFence.Core.Http;

/// <summary>
/// HTTP probe client implementation providing diagnostic request dispatching and timing.
/// </summary>
public sealed class ProbeClient : IProbeClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly TargetConfig _targetConfig;
    private readonly Uri _baseUri;
    private readonly Uri _probeUri;

    public Uri TargetBaseUri => _baseUri;
    public Uri TargetProbeUri => _probeUri;

    public ProbeClient(TargetConfig targetConfig, HttpClient? customHttpClient = null)
    {
        _targetConfig = targetConfig ?? throw new ArgumentNullException(nameof(targetConfig));
        _baseUri = new Uri(targetConfig.BaseUrl.TrimEnd('/') + "/");

        string probePath = string.IsNullOrWhiteSpace(targetConfig.ProbePath)
            ? "/_hopfence/probe"
            : targetConfig.ProbePath.TrimStart('/');

        _probeUri = new Uri(_baseUri, probePath);

        if (customHttpClient != null)
        {
            _httpClient = customHttpClient;
            _ownsHttpClient = false;
        }
        else
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false // We inspect redirects manually in redirect tests
            };

            if (targetConfig.Insecure)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(targetConfig.TimeoutSeconds > 0 ? targetConfig.TimeoutSeconds : 15)
            };
            _ownsHttpClient = true;
        }
    }

    public async Task<ProbeExecutionResult> ExecuteProbeAsync(
        IDictionary<string, string>? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ProbeExecutionResult();
        var sw = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _probeUri);

            // Add Authorization token if configured
            if (!string.IsNullOrWhiteSpace(_targetConfig.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _targetConfig.Token);
            }

            // User-defined static headers
            foreach (var kvp in _targetConfig.Headers)
            {
                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }

            // Diagnostic custom headers (e.g. spoofed X-Forwarded-For, X-Forwarded-Proto, etc.)
            if (customHeaders != null)
            {
                foreach (var kvp in customHeaders)
                {
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }

            // Add standard HopFence user agent
            request.Headers.UserAgent.ParseAdd("HopFence-Diagnostics/1.0 (+https://kadircanuredi.com.tr)");

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            result.StatusCode = response.StatusCode;
            result.RawResponse = response;

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            result.RawBody = body;

            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    result.ProbeData = JsonSerializer.Deserialize<ProbeResponse>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    result.ErrorMessage = $"Failed to parse JSON response from probe: {ex.Message}";
                }
            }
            else if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"Probe endpoint returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Diagnostic request failed: {ex.Message}";
        }
        finally
        {
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<HttpResponseMessage> SendRawAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
