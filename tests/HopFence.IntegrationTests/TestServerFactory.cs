using System.Net;
using HopFence.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HopFence.IntegrationTests;

public static class TestServerFactory
{
    public const string TestToken = "hopfence-test-secret-token-xyz";
    public const string RealClientIp = "203.0.113.10";

    /// <summary>
    /// Creates a properly configured, secure ASP.NET Core server behind a simulated secure reverse proxy.
    /// The proxy overwrites/sanitizes untrusted client headers (e.g. proxy_set_header X-Forwarded-For $remote_addr).
    /// </summary>
    public static TestServer CreateSecureServer()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Configure(app =>
            {
                // Simulated Secure Reverse Proxy Layer (e.g. Nginx with proxy_set_header)
                app.Use(async (context, next) =>
                {
                    // A secure proxy overrides client-injected headers with the genuine client remote IP
                    context.Request.Headers["X-Forwarded-For"] = RealClientIp;
                    context.Request.Headers["X-Forwarded-Proto"] = "https";
                    context.Request.Headers["X-Forwarded-Host"] = "staging.example.com";
                    context.Connection.RemoteIpAddress = IPAddress.Loopback; // proxy socket to backend
                    await next();
                });

                // HopFence captures socket peer IP before ForwardedHeadersMiddleware
                app.UseHopFenceCapture();

                var fwdOptions = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.All,
                    RequireHeaderSymmetry = false,
                    ForwardLimit = 1
                };
                fwdOptions.KnownNetworks.Clear();
                fwdOptions.KnownProxies.Clear();
                fwdOptions.KnownProxies.Add(IPAddress.Loopback);

                app.UseForwardedHeaders(fwdOptions);

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHopFenceProbe(options =>
                    {
                        options.Enabled = true;
                        options.Token = TestToken;
                    });
                });
            });

        return new TestServer(builder);
    }

    /// <summary>
    /// Creates a misconfigured, vulnerable ASP.NET Core server that blindly trusts client-injected X-Forwarded-For headers.
    /// </summary>
    public static TestServer CreateVulnerableServer()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Configure(app =>
            {
                // Simulated Misconfigured Proxy: Passes client's raw XFF directly without sanitization
                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Loopback;
                    await next();
                });

                app.UseHopFenceCapture();

                // Vulnerable backend: Clears known proxies and networks, accepting any client-supplied header
                var fwdOptions = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.All
                };
                fwdOptions.KnownNetworks.Clear();
                fwdOptions.KnownProxies.Clear();

                app.UseForwardedHeaders(fwdOptions);

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHopFenceProbe(options =>
                    {
                        options.Enabled = true;
                        options.Token = TestToken;
                    });
                });
            });

        return new TestServer(builder);
    }

    /// <summary>
    /// Creates a server where HopFence probe is disabled (default state).
    /// </summary>
    public static TestServer CreateDisabledProbeServer()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services => services.AddRouting())
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHopFenceProbe(options =>
                    {
                        options.Enabled = false; // explicitly disabled
                    });
                });
            });

        return new TestServer(builder);
    }
}
