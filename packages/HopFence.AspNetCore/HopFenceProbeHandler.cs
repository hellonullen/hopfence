using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HopFence.Core.Models;
using Microsoft.AspNetCore.Http;

namespace HopFence.AspNetCore;

/// <summary>
/// Secure handler for the HopFence probe diagnostic endpoint.
/// </summary>
public static class HopFenceProbeHandler
{
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> RateLimits = new();

    public static async Task HandleAsync(HttpContext context, HopFenceOptions options)
    {
        // 1. Check if enabled
        bool isEnabled = options.Enabled ||
            string.Equals(Environment.GetEnvironmentVariable("HOPFENCE_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);

        if (!isEnabled)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        // 2. Set strict anti-caching headers
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";

        // 3. Optional IP allowlist check
        string? peerIp = context.Connection.RemoteIpAddress?.ToString();
        if (options.AllowedIps.Count > 0 && !string.IsNullOrEmpty(peerIp))
        {
            if (!options.AllowedIps.Contains(peerIp))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Forbidden: IP address not allowed to access HopFence probe.");
                return;
            }
        }

        // 4. Rate limiting check
        if (options.RateLimitPerMinute > 0 && !string.IsNullOrEmpty(peerIp))
        {
            if (IsRateLimited(peerIp, options.RateLimitPerMinute))
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("Too Many Requests: Rate limit exceeded.");
                return;
            }
        }

        // 5. Authenticate Bearer Token with timing-attack resistant comparison
        string? expectedToken = options.Token ?? Environment.GetEnvironmentVariable("HOPFENCE_TOKEN");
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "Bearer error=\"token_not_configured\"";
            await context.Response.WriteAsync("Unauthorized: HOPFENCE_TOKEN is not configured on the target server.");
            return;
        }

        string authHeader = context.Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "Bearer";
            await context.Response.WriteAsync("Unauthorized: Missing Bearer token.");
            return;
        }

        string providedToken = authHeader.Substring("Bearer ".Length).Trim();
        if (!FixedTimeEquals(providedToken, expectedToken))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "Bearer error=\"invalid_token\"";
            await context.Response.WriteAsync("Unauthorized: Invalid Bearer token.");
            return;
        }

        // 6. Assemble Probe Response
        string? socketPeerIp = context.Items.TryGetValue(HopFenceCaptureMiddleware.SocketPeerIpItemKey, out var itemIp)
            ? itemIp?.ToString()
            : context.Connection.RemoteIpAddress?.ToString();

        var probeResponse = new ProbeResponse
        {
            SchemaVersion = 1,
            Framework = "aspnetcore",
            Request = new ProbeRequestData
            {
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                SocketPeerIp = socketPeerIp,
                Scheme = context.Request.Scheme,
                Host = context.Request.Host.Value,
                PathBase = context.Request.PathBase.Value ?? string.Empty,
                ForwardedHeaders = new ForwardedHeadersInfo
                {
                    XForwardedForPresent = context.Request.Headers.ContainsKey("X-Forwarded-For"),
                    XForwardedProtoPresent = context.Request.Headers.ContainsKey("X-Forwarded-Proto"),
                    XForwardedHostPresent = context.Request.Headers.ContainsKey("X-Forwarded-Host"),
                    RawXForwardedFor = context.Request.Headers["X-Forwarded-For"].ToString(),
                    RawXForwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString(),
                    RawXForwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString()
                }
            }
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        await JsonSerializer.SerializeAsync(context.Response.Body, probeResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Timing-attack resistant string comparison.
    /// </summary>
    public static bool FixedTimeEquals(string a, string b)
    {
        if (a == null || b == null) return false;

        byte[] aBytes = Encoding.UTF8.GetBytes(a);
        byte[] bBytes = Encoding.UTF8.GetBytes(b);

        if (aBytes.Length != bBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static bool IsRateLimited(string ip, int maxPerMinute)
    {
        var now = DateTime.UtcNow;
        var entry = RateLimits.AddOrUpdate(ip,
            _ => (1, now),
            (_, current) =>
            {
                if ((now - current.WindowStart).TotalSeconds > 60)
                {
                    return (1, now);
                }
                return (current.Count + 1, current.WindowStart);
            });

        return entry.Count > maxPerMinute;
    }
}
