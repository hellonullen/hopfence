# HopFence

> **Reverse Proxy Diagnostics & Security Testing Tool**  
> *Developed by **NulleN - Kadircan Üredi***  
> **Website:** [kadircanuredi.com.tr](https://kadircanuredi.com.tr) &bull; **Contact:** [hellonullen@gmail.com](mailto:hellonullen@gmail.com)

---

## Overview

**HopFence** is a specialized open-source security audit and diagnostic utility designed to test web applications running behind reverse proxies and load balancers (such as Nginx, Traefik, HAProxy, AWS ALB, and Cloudflare).

Rather than acting as a proxy itself, HopFence inspects whether downstream application servers (ASP.NET Core, Express.js) correctly and securely interpret forwarded headers, identify genuine client IP addresses, preserve original request schemes, and effectively prevent header injection attacks.

---

## Key Diagnostic Checks

HopFence automates a comprehensive battery of security and configuration checks:

| Code | Test Name | Description |
|---|---|---|
| **HOP-HTTPS-001** | **HTTPS Detection** | Validates that backend applications detect connection scheme as HTTPS behind SSL-terminating proxies. |
| **HOP-IP-001** | **Client IP Detection** | Asserts client IP calculation against expected address or flags unrouted/loopback configurations. |
| **HOP-SPOOF-XFF-001** | **X-Forwarded-For Protection** | Simulates header injection with untrusted client IPs to identify unvalidated proxy chains. |
| **HOP-SPOOF-PROTO-001** | **Proto Downgrade Protection** | Injects `X-Forwarded-Proto: http` over TLS to detect protocol downgrade vulnerabilities. |
| **HOP-SPOOF-HOST-001** | **Host Header Poisoning** | Injects untrusted `X-Forwarded-Host` values to protect against password-reset and cache poisoning. |
| **HOP-REDIR-001** | **Redirect Enforcement** | Checks HTTP-to-HTTPS redirect loops, hop limits (max 5), and external host boundary leakage. |
| **HOP-PATHBASE-001** | **Subpath Preservation** | Validates PathBase retention when applications run behind subpath prefixes (e.g., `/app` or `/api`). |

---

## Monorepo Architecture

```text
HopFence/
├── src/
│   ├── HopFence.Cli/          # Command-line interface (.NET 9.0)
│   ├── HopFence.Core/         # Diagnostic engine, models, and test battery
│   └── HopFence.Reporting/    # Multi-format report generators (Console, JSON, HTML, JUnit)
├── packages/
│   ├── HopFence.AspNetCore/   # NuGet middleware for ASP.NET Core
│   └── hopfence-express/      # NPM middleware for Express.js
├── examples/
│   ├── aspnetcore-nginx/      # Example ASP.NET Core service behind Nginx
│   └── express-nginx/         # Example Express service behind Nginx
├── tests/
│   ├── HopFence.UnitTests/    # Unit tests for models, config, and reporters
│   └── HopFence.IntegrationTests/ # End-to-end tests with in-memory TestServer
├── .github/workflows/         # Cross-platform CI automation
├── docker-compose.yml         # Containerized local test lab
├── LICENSE                    # Open-source license
└── README.md
```

---

## CLI Usage

### Installation & Execution

```bash
# Display help and usage
hopfence --help

# Generate starter configuration (hopfence.yml)
hopfence init

# Run full diagnostics against target
hopfence diagnose

# Run diagnostics with custom output format
hopfence diagnose --format html --output report.html
hopfence diagnose --format junit --output results.xml
hopfence diagnose --format json --output report.json

# Direct command-line overrides
hopfence diagnose --target https://staging.example.com --token my-secret-token --insecure
```

### Configuration (`hopfence.yml`)

```yaml
version: 1

target:
  baseUrl: "https://staging.example.com"
  probePath: "/_hopfence/probe"
  tokenEnv: "HOPFENCE_TOKEN"
  insecure: false
  timeoutSeconds: 15
  maxRedirects: 5

expect:
  scheme: "https"
  host: "staging.example.com"
  pathBase: ""
  clientIp: null

tests:
  httpsDetection: true
  clientIpDetection: true
  forwardedForSpoofing: true
  forwardedProtoSpoofing: true
  forwardedHostSpoofing: true
  redirect: true
  pathBase: true

reporting:
  format: "console"
  outputFile: null
  verbose: false
```

---

## ASP.NET Core Integration

Add the package reference to your project:

```bash
dotnet add package HopFence.AspNetCore
```

Configure your application pipeline in `Program.cs`:

```csharp
using HopFence.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Capture raw socket connection IP BEFORE ForwardedHeadersMiddleware
app.UseHopFenceCapture();

// 2. Process reverse proxy forwarded headers
app.UseForwardedHeaders();

// 3. Mount secure HopFence probe endpoint (/_hopfence/probe)
app.MapHopFenceProbe(options =>
{
    options.Enabled = builder.Configuration.GetValue<bool>("HopFence:Enabled");
    options.Token = builder.Configuration.GetValue<string>("HopFence:Token");
});

app.Run();
```

---

## Express.js Integration

Install the package via npm:

```bash
npm install @hopfence/express
```

Register the middleware in your Express application:

```javascript
const express = require('express');
const hopFence = require('@hopfence/express');

const app = express();

// Set your trusted proxy configuration
app.set('trust proxy', 1);

// Mount HopFence diagnostic middleware
app.use(hopFence({
  enabled: process.env.HOPFENCE_ENABLED === 'true',
  token: process.env.HOPFENCE_TOKEN
}));

app.listen(3000);
```

---

## Diagnostic Probe Endpoint Security

Both ASP.NET Core and Express adapters implement enterprise-grade security controls:

1. **Disabled by default**: Endpoints return `404 Not Found` unless explicitly enabled via configuration or `HOPFENCE_ENABLED=true`.
2. **Bearer Token Authentication**: Enforces `Authorization: Bearer <token>` verified using constant-time string comparison (`CryptographicOperations.FixedTimeEquals` in C# and `crypto.timingSafeEqual` in Node.js) to thwart timing attacks.
3. **No Sensitive Data Exposure**: Endpoint payloads strictly omit HTTP cookies, request bodies, query strings, and authentication tokens.
4. **Anti-Caching Headers**: Responses mandate `Cache-Control: no-store, no-cache, must-revalidate`.
5. **Rate Limiting**: Built-in sliding-window limiter throttles requests per client IP.

### Standardized Response Schema (`schemaVersion: 1`)

```json
{
  "schemaVersion": 1,
  "framework": "aspnetcore",
  "request": {
    "clientIp": "203.0.113.10",
    "socketPeerIp": "172.20.0.5",
    "scheme": "https",
    "host": "staging.example.com",
    "pathBase": "",
    "forwardedHeaders": {
      "xForwardedForPresent": true,
      "xForwardedProtoPresent": true,
      "xForwardedHostPresent": true,
      "rawXForwardedFor": "203.0.113.10",
      "rawXForwardedProto": "https",
      "rawXForwardedHost": "staging.example.com"
    }
  }
}
```

---

## Author & Credits

- **Creator & Lead Developer:** **NulleN - Kadircan Üredi**
- **Website:** [https://kadircanuredi.com.tr](https://kadircanuredi.com.tr)
- **E-Mail:** [hellonullen@gmail.com](mailto:hellonullen@gmail.com)
- **GitHub Repository:** [https://github.com/hellonullen/hopfence](https://github.com/hellonullen/hopfence)
