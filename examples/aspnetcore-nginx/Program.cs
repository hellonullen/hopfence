using System.Net;
using HopFence.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure trusted forward headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;

    // Trust Docker internal network or local proxy
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    options.KnownProxies.Add(IPAddress.Loopback);
});

var app = builder.Build();

// 1. HopFence socket IP capture (MUST precede UseForwardedHeaders)
app.UseHopFenceCapture();

// 2. Apply forwarded headers from reverse proxy
app.UseForwardedHeaders();

// 3. Optional HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 4. Sample application routes
app.MapGet("/", (HttpContext context) =>
{
    return Results.Ok(new
    {
        message = "HopFence ASP.NET Core Sample App running behind Nginx",
        clientIp = context.Connection.RemoteIpAddress?.ToString(),
        scheme = context.Request.Scheme,
        host = context.Request.Host.Value
    });
});

// 5. Expose HopFence diagnostic probe endpoint
app.MapHopFenceProbe(options =>
{
    // Enabled via environment variable HOPFENCE_ENABLED=true
    options.Enabled = builder.Configuration.GetValue<bool>("HopFence:Enabled");
    options.Token = builder.Configuration.GetValue<string>("HopFence:Token");
});

app.Run();
