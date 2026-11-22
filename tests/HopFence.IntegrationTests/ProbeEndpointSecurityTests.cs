using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using HopFence.Core.Models;
using Xunit;

namespace HopFence.IntegrationTests;

public class ProbeEndpointSecurityTests
{
    [Fact]
    public async Task ProbeEndpoint_WhenDisabled_Returns404NotFound()
    {
        using var server = TestServerFactory.CreateDisabledProbeServer();
        using var client = server.CreateClient();

        var response = await client.GetAsync("/_hopfence/probe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProbeEndpoint_WithoutToken_Returns401Unauthorized()
    {
        using var server = TestServerFactory.CreateSecureServer();
        using var client = server.CreateClient();

        var response = await client.GetAsync("/_hopfence/probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.WwwAuthenticate.Count > 0);
    }

    [Fact]
    public async Task ProbeEndpoint_WithInvalidToken_Returns401Unauthorized()
    {
        using var server = TestServerFactory.CreateSecureServer();
        using var client = server.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-token-abc");
        var response = await client.GetAsync("/_hopfence/probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProbeEndpoint_WithValidToken_Returns200WithSchemaV1()
    {
        using var server = TestServerFactory.CreateSecureServer();
        using var client = server.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestServerFactory.TestToken);
        var response = await client.GetAsync("/_hopfence/probe");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        var probeResponse = JsonSerializer.Deserialize<ProbeResponse>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(probeResponse);
        Assert.Equal(1, probeResponse.SchemaVersion);
        Assert.Equal("aspnetcore", probeResponse.Framework);
        Assert.NotNull(probeResponse.Request);
    }
}
