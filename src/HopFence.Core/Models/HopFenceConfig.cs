using System.Collections.Generic;

namespace HopFence.Core.Models;

/// <summary>
/// Root configuration file structure for HopFence.
/// </summary>
public sealed class HopFenceConfig
{
    public int Version { get; set; } = 1;

    public TargetConfig Target { get; set; } = new();

    public ExpectConfig Expect { get; set; } = new();

    public TestsConfig Tests { get; set; } = new();

    public ReportingConfig Reporting { get; set; } = new();
}

public sealed class TargetConfig
{
    /// <summary>Base URL of the target application (e.g. "https://staging.example.com").</summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>Relative or absolute path to the HopFence diagnostic probe endpoint.</summary>
    public string ProbePath { get; set; } = "/_hopfence/probe";

    /// <summary>Direct bearer token string. If omitted, HopFence checks TokenEnv.</summary>
    public string? Token { get; set; }

    /// <summary>Environment variable name supplying the bearer token.</summary>
    public string? TokenEnv { get; set; } = "HOPFENCE_TOKEN";

    /// <summary>Ignore SSL/TLS certificate errors (useful for staging/self-signed certs).</summary>
    public bool Insecure { get; set; } = false;

    /// <summary>HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Maximum number of HTTP redirect hops to follow.</summary>
    public int MaxRedirects { get; set; } = 5;

    /// <summary>Optional custom HTTP headers to include with requests.</summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}

public sealed class ExpectConfig
{
    /// <summary>Expected scheme (typically "https").</summary>
    public string? Scheme { get; set; } = "https";

    /// <summary>Expected Host header perceived by the backend application.</summary>
    public string? Host { get; set; }

    /// <summary>Expected path base (e.g. "/app" or "/api") if deployed behind subpath routing.</summary>
    public string? PathBase { get; set; }

    /// <summary>Expected public client IP. If null, HopFence issues WARN/INFO rather than strict assertion.</summary>
    public string? ClientIp { get; set; }
}

public sealed class TestsConfig
{
    /// <summary>A. Verify application detects HTTPS correctly.</summary>
    public bool HttpsDetection { get; set; } = true;

    /// <summary>B. Verify application resolves client IP properly.</summary>
    public bool ClientIpDetection { get; set; } = true;

    /// <summary>C. Test against client-supplied X-Forwarded-For injection/spoofing.</summary>
    public bool ForwardedForSpoofing { get; set; } = true;

    /// <summary>D. Test against protocol downgrade via X-Forwarded-Proto.</summary>
    public bool ForwardedProtoSpoofing { get; set; } = true;

    /// <summary>E. Test against Host header poisoning via X-Forwarded-Host.</summary>
    public bool ForwardedHostSpoofing { get; set; } = true;

    /// <summary>F. Test HTTP to HTTPS redirection and loop safety.</summary>
    public bool Redirect { get; set; } = true;

    /// <summary>G. Test subpath / PathBase preservation.</summary>
    public bool PathBase { get; set; } = true;
}

public sealed class ReportingConfig
{
    /// <summary>Report output format: console, json, html, junit.</summary>
    public string Format { get; set; } = "console";

    /// <summary>File path to save the generated report.</summary>
    public string? OutputFile { get; set; }

    /// <summary>Enable verbose diagnostic console logging.</summary>
    public bool Verbose { get; set; } = false;
}
