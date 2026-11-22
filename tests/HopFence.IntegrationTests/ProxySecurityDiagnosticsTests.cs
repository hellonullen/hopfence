using System.Linq;
using System.Threading.Tasks;
using HopFence.Core.Engine;
using HopFence.Core.Http;
using HopFence.Core.Models;
using HopFence.Core.Tests;
using Xunit;

namespace HopFence.IntegrationTests;

public class ProxySecurityDiagnosticsTests
{
    [Fact]
    public async Task VulnerableProxy_FailsForwardedForSpoofingTest()
    {
        using var server = TestServerFactory.CreateVulnerableServer();
        using var httpClient = server.CreateClient();

        var config = new HopFenceConfig();
        config.Target.BaseUrl = "http://localhost/";
        config.Target.Token = TestServerFactory.TestToken;
        config.Tests.Redirect = false; // Disable redirect test for in-memory host

        var probeClient = new ProbeClient(config.Target, httpClient);
        var engine = new DiagnosticEngine();

        var report = await engine.RunDiagnosticsAsync(config, probeClient);

        // Assert that the spoofing test detected the vulnerability!
        var spoofTest = report.Results.FirstOrDefault(r => r.TestId == "HOP-SPOOF-XFF-001");
        Assert.NotNull(spoofTest);
        Assert.Equal(TestStatus.Fail, spoofTest.Status);
        Assert.Equal(TestSeverity.Critical, spoofTest.Severity);
        Assert.Contains("CRITICAL VULNERABILITY", spoofTest.Details);
        Assert.False(report.IsSuccess);
    }

    [Fact]
    public async Task SecureProxy_PassesForwardedForSpoofingTest()
    {
        using var server = TestServerFactory.CreateSecureServer();
        using var httpClient = server.CreateClient();

        var config = new HopFenceConfig();
        config.Target.BaseUrl = "http://localhost/";
        config.Target.Token = TestServerFactory.TestToken;
        config.Tests.Redirect = false; // Disable redirect test for in-memory host

        var probeClient = new ProbeClient(config.Target, httpClient);
        var engine = new DiagnosticEngine();

        var report = await engine.RunDiagnosticsAsync(config, probeClient);

        // Assert that spoofing test PASSED because the server did not accept the spoofed IP!
        var spoofTest = report.Results.FirstOrDefault(r => r.TestId == "HOP-SPOOF-XFF-001");
        Assert.NotNull(spoofTest);
        Assert.Equal(TestStatus.Pass, spoofTest.Status);
    }
}
