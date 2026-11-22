using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace HopFence.AspNetCore;

/// <summary>
/// IApplicationBuilder and IEndpointRouteBuilder extension methods for integrating HopFence diagnostics.
/// </summary>
public static class HopFenceExtensions
{
    /// <summary>
    /// Captures the raw socket connection IP before any forwarded headers middleware rewrites it.
    /// Place this BEFORE app.UseForwardedHeaders().
    /// </summary>
    public static IApplicationBuilder UseHopFenceCapture(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HopFenceCaptureMiddleware>();
    }

    /// <summary>
    /// Maps the HopFence diagnostic probe endpoint (defaults to "/_hopfence/probe").
    /// </summary>
    public static IEndpointConventionBuilder MapHopFenceProbe(
        this IEndpointRouteBuilder endpoints,
        Action<HopFenceOptions>? configure = null)
    {
        var options = new HopFenceOptions();
        configure?.Invoke(options);

        string pattern = string.IsNullOrWhiteSpace(options.ProbePath)
            ? "/_hopfence/probe"
            : "/" + options.ProbePath.TrimStart('/');

        return endpoints.MapGet(pattern, context => HopFenceProbeHandler.HandleAsync(context, options))
            .WithDisplayName("HopFence Diagnostic Probe");
    }

    /// <summary>
    /// Maps the HopFence diagnostic probe endpoint on IApplicationBuilder directly.
    /// </summary>
    public static IApplicationBuilder MapHopFenceProbe(
        this IApplicationBuilder app,
        Action<HopFenceOptions>? configure = null)
    {
        var options = new HopFenceOptions();
        configure?.Invoke(options);

        string pattern = string.IsNullOrWhiteSpace(options.ProbePath)
            ? "/_hopfence/probe"
            : "/" + options.ProbePath.TrimStart('/');

        app.Map(pattern, subApp =>
        {
            subApp.Run(context => HopFenceProbeHandler.HandleAsync(context, options));
        });

        return app;
    }
}
