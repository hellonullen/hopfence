using System;
using System.Collections.Generic;

namespace HopFence.AspNetCore;

/// <summary>
/// Configuration options for HopFence ASP.NET Core diagnostic adapter.
/// </summary>
public sealed class HopFenceOptions
{
    /// <summary>
    /// Whether the diagnostic endpoint is enabled. Defaults to false.
    /// Can also be toggled via environment variable HOPFENCE_ENABLED=true.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Bearer authentication token required to query the probe endpoint.
    /// Can also be provided via environment variable HOPFENCE_TOKEN.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Path for the diagnostic probe endpoint. Defaults to "/_hopfence/probe".
    /// </summary>
    public string ProbePath { get; set; } = "/_hopfence/probe";

    /// <summary>
    /// Optional IP allowlist. If non-empty, only requests originating from these IP addresses can query the probe.
    /// </summary>
    public HashSet<string> AllowedIps { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maximum probe requests allowed per minute per peer IP (default: 60).
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;
}
