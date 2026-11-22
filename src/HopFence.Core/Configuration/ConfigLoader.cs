using System;
using System.IO;
using System.Text.RegularExpressions;
using HopFence.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HopFence.Core.Configuration;

/// <summary>
/// Loads and validates HopFence configuration from YAML or JSON with environment variable expansion.
/// </summary>
public static class ConfigLoader
{
    private static readonly Regex EnvVarRegex = new(@"\$\{([a-zA-Z0-9_]+)(?::-([^}]*))?\}", RegexOptions.Compiled);

    /// <summary>
    /// Loads configuration from a file path (YAML or JSON).
    /// </summary>
    public static HopFenceConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: '{filePath}'");
        }

        string rawContent = File.ReadAllText(filePath);
        return LoadFromString(rawContent, Path.GetExtension(filePath));
    }

    /// <summary>
    /// Loads configuration from raw string content.
    /// </summary>
    public static HopFenceConfig LoadFromString(string content, string? extensionHint = null)
    {
        string expandedContent = ExpandEnvironmentVariables(content);

        HopFenceConfig config;
        bool isJson = (extensionHint != null && extensionHint.Equals(".json", StringComparison.OrdinalIgnoreCase))
                      || expandedContent.TrimStart().StartsWith("{", StringComparison.Ordinal);

        if (isJson)
        {
            config = System.Text.Json.JsonSerializer.Deserialize<HopFenceConfig>(expandedContent,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new HopFenceConfig();
        }
        else
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            config = deserializer.Deserialize<HopFenceConfig>(expandedContent) ?? new HopFenceConfig();
        }

        // Resolve token from TokenEnv if direct Token is empty
        if (string.IsNullOrWhiteSpace(config.Target.Token) && !string.IsNullOrWhiteSpace(config.Target.TokenEnv))
        {
            config.Target.Token = Environment.GetEnvironmentVariable(config.Target.TokenEnv);
        }

        ValidateConfig(config);
        return config;
    }

    /// <summary>
    /// Replaces occurrences of ${VAR} or ${VAR:-default} with environment variable values.
    /// </summary>
    public static string ExpandEnvironmentVariables(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return EnvVarRegex.Replace(input, match =>
        {
            string varName = match.Groups[1].Value;
            string defaultValue = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
            string? value = Environment.GetEnvironmentVariable(varName);

            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        });
    }

    private static void ValidateConfig(HopFenceConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Target.BaseUrl))
        {
            throw new ArgumentException("Configuration error: 'target.baseUrl' cannot be empty.");
        }

        if (!Uri.TryCreate(config.Target.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Configuration error: 'target.baseUrl' must be a valid HTTP or HTTPS URI. Given: '{config.Target.BaseUrl}'");
        }
    }

    /// <summary>
    /// Generates a starter YAML configuration with thorough documentation comments.
    /// </summary>
    public static string GenerateDefaultConfigYaml()
    {
        return @"# ==============================================================================
# HopFence — Reverse Proxy Diagnostics & Security Testing Configuration
# Author: NulleN - Kadircan Üredi (kadircanuredi.com.tr / hellonullen@gmail.com)
# ==============================================================================
version: 1

target:
  # Base URL of your web application or reverse proxy entrypoint
  baseUrl: ""https://staging.example.com""

  # Probe endpoint exposed by HopFence.AspNetCore or @hopfence/express
  probePath: ""/_hopfence/probe""

  # Environment variable name holding the Bearer authentication token
  tokenEnv: ""HOPFENCE_TOKEN""

  # Or specify the token directly (not recommended for version control)
  # token: ""super-secret-token-change-in-production""

  # Set true to bypass SSL/TLS verification (e.g. self-signed certs in local dev)
  insecure: false

  # Request timeout in seconds
  timeoutSeconds: 15

  # Maximum HTTP redirect hops allowed
  maxRedirects: 5

expect:
  # Protocol perceived by the target application behind the reverse proxy
  scheme: ""https""

  # Host header perceived by the backend application
  host: ""staging.example.com""

  # Base path prefix if your application runs under a subpath (e.g. /app or /api)
  pathBase: """"

  # Expected client IP (leave null to perform heuristic check without strict equality)
  clientIp: null

tests:
  # Test A: Check whether backend recognizes HTTPS protocol correctly
  httpsDetection: true

  # Test B: Verify client IP resolution
  clientIpDetection: true

  # Test C: Inject spoofed X-Forwarded-For header to verify proxy sanitization
  forwardedForSpoofing: true

  # Test D: Inject spoofed X-Forwarded-Proto header to verify protocol downgrade protection
  forwardedProtoSpoofing: true

  # Test E: Inject spoofed X-Forwarded-Host header to verify Host poisoning protection
  forwardedHostSpoofing: true

  # Test F: Test HTTP -> HTTPS redirection, redirect loops, and hop limits
  redirect: true

  # Test G: Verify subpath preservation
  pathBase: true

reporting:
  # Supported formats: console, json, html, junit
  format: ""console""

  # Output path for file-based reports (e.g. hopfence-report.html)
  outputFile: null

  # Enable verbose diagnostic logging
  verbose: false
";
    }
}
