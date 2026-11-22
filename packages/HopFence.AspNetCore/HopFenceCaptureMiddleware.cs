using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HopFence.AspNetCore;

/// <summary>
/// Middleware placed BEFORE UseForwardedHeaders to capture the true underlying socket peer IP address.
/// </summary>
public sealed class HopFenceCaptureMiddleware
{
    public const string SocketPeerIpItemKey = "HopFence.SocketPeerIp";

    private readonly RequestDelegate _next;

    public HopFenceCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Record the raw socket connection IP before any middleware rewrites RemoteIpAddress
        var socketIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(socketIp))
        {
            context.Items[SocketPeerIpItemKey] = socketIp;
        }

        await _next(context);
    }
}
