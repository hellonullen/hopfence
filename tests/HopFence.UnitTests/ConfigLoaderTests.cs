using System;
using HopFence.Core.Configuration;
using Xunit;

namespace HopFence.UnitTests;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadFromString_ValidYaml_ParsesCorrectly()
    {
        string yaml = @"
version: 1
target:
  baseUrl: ""https://api.example.com""
  probePath: ""/_hopfence/probe""
  token: ""secret123""
expect:
  scheme: ""https""
  host: ""api.example.com""
tests:
  forwardedForSpoofing: true
  redirect: false
reporting:
  format: ""json""
";

        var config = ConfigLoader.LoadFromString(yaml, ".yml");

        Assert.Equal(1, config.Version);
        Assert.Equal("https://api.example.com", config.Target.BaseUrl);
        Assert.Equal("/_hopfence/probe", config.Target.ProbePath);
        Assert.Equal("secret123", config.Target.Token);
        Assert.Equal("https", config.Expect.Scheme);
        Assert.Equal("api.example.com", config.Expect.Host);
        Assert.True(config.Tests.ForwardedForSpoofing);
        Assert.False(config.Tests.Redirect);
        Assert.Equal("json", config.Reporting.Format);
    }

    [Fact]
    public void ExpandEnvironmentVariables_ReplacesVariablesCorrectly()
    {
        Environment.SetEnvironmentVariable("TEST_HOPFENCE_VAR", "injected_token_value");

        string template = "baseUrl: \"https://example.com\"\ntoken: \"${TEST_HOPFENCE_VAR}\"\ndefaulted: \"${MISSING_VAR:-fallback}\"";
        string expanded = ConfigLoader.ExpandEnvironmentVariables(template);

        Assert.Contains("injected_token_value", expanded);
        Assert.Contains("fallback", expanded);
    }

    [Fact]
    public void LoadFromString_InvalidBaseUrl_ThrowsArgumentException()
    {
        string yaml = @"
target:
  baseUrl: ""not-a-valid-url""
";

        Assert.Throws<ArgumentException>(() => ConfigLoader.LoadFromString(yaml));
    }

    [Fact]
    public void GenerateDefaultConfigYaml_ContainsAuthorAndTarget()
    {
        string defaultYaml = ConfigLoader.GenerateDefaultConfigYaml();

        Assert.Contains("HopFence", defaultYaml);
        Assert.Contains("kadircanuredi.com.tr", defaultYaml);
        Assert.Contains("baseUrl", defaultYaml);
    }
}
