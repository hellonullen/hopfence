# HopFence Architecture Overview

HopFence is designed as a modular developer tool and security audit suite for reverse proxy environments.

## Component Breakdown

### 1. `HopFence.Core`
Framework-agnostic testing engine, HTTP probe client, configuration loader, and diagnostic rules battery.
- `DiagnosticEngine`: Orchestrates test execution against target probes.
- `ProbeClient`: Dispatches authenticated HTTP requests with arbitrary header mutations.
- `ConfigLoader`: Parses YAML/JSON configs with environment variable interpolation (`${VAR:-default}`).

### 2. `HopFence.Reporting`
Multi-format reporting engine supporting:
- **ConsoleReporter**: ANSI color highlights, formatted tables, and inline remediation advice.
- **JsonReporter**: Machine-readable JSON output for automated pipelines.
- **HtmlReporter**: Single-file dark-themed dashboard with SVG metrics and zero external dependencies.
- **JUnitReporter**: Standardized JUnit XML for CI/CD test visualizers.

### 3. `HopFence.Cli`
Cross-platform command-line utility (.NET 9.0) supporting `init`, `diagnose`, and `version` commands with flexible flag overrides.

### 4. `HopFence.AspNetCore`
Official NuGet package providing:
- `app.UseHopFenceCapture()`: Records raw socket peer IP prior to `UseForwardedHeaders`.
- `app.MapHopFenceProbe()`: Exposes `/_hopfence/probe` with timing-safe Bearer authentication.

### 5. `@hopfence/express`
Official npm package providing zero-dependency Express middleware for inspecting `req.ip`, `req.ips`, `req.protocol`, `req.hostname`, and proxy header presence.

---

**Author:** NulleN - Kadircan Üredi ([kadircanuredi.com.tr](https://kadircanuredi.com.tr) / hellonullen@gmail.com)
