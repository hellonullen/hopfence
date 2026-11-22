# HopFence Diagnostic Test Vectors

HopFence evaluates reverse proxy configurations against seven diagnostic vectors:

## 1. HOP-HTTPS-001 (HTTPS Detection)
- **Objective:** Asserts that backend applications recognize the incoming request scheme as `https`.
- **Vulnerability:** If perceived as `http`, applications may emit insecure cookies, generate broken redirect loops, or fail security checks.
- **Remediation:** Configure `proxy_set_header X-Forwarded-Proto $scheme;` in Nginx and configure `UseForwardedHeaders` in ASP.NET Core or `app.set('trust proxy', ...)` in Express.

## 2. HOP-IP-001 (Client IP Resolution)
- **Objective:** Validates whether the application correctly calculates the genuine client IP address.
- **Evaluation:** Matches against `expect.clientIp` if provided, or flags loopback/socket address ambiguities as warnings.

## 3. HOP-SPOOF-XFF-001 (X-Forwarded-For Protection)
- **Objective:** Injects `X-Forwarded-For: 198.51.100.77` to detect IP spoofing.
- **Vulnerability:** When proxies fail to sanitize client headers, attackers can bypass IP allowlists, geo-blocks, and rate limiters.
- **Remediation:** In Nginx, use `proxy_set_header X-Forwarded-For $remote_addr;` or define explicit `KnownProxies`/`KnownNetworks` in ASP.NET Core.

## 4. HOP-SPOOF-PROTO-001 (Protocol Downgrade Protection)
- **Objective:** Injects `X-Forwarded-Proto: http` over an established HTTPS connection.
- **Vulnerability:** Applications may downgrade transport security context, causing SSL stripping vulnerabilities.
- **Remediation:** Force reverse proxies to override client-supplied proto headers with `$scheme`.

## 5. HOP-SPOOF-HOST-001 (Host Header Poisoning Protection)
- **Objective:** Injects `X-Forwarded-Host: attacker.hopfence.invalid`.
- **Vulnerability:** Applications trusting untrusted hosts are vulnerable to Host Header Injection, password-reset token leakage, and cache poisoning.
- **Remediation:** Restrict allowed hosts in ASP.NET Core (`AllowedHosts`) and configure `proxy_set_header Host $host;` in Nginx.

## 6. HOP-REDIR-001 (Redirect & Hop Limit Enforcement)
- **Objective:** Inspects HTTP-to-HTTPS redirect behaviors, verifies redirect location host boundaries, and prevents redirect loops.
- **Rules:** Enforces a maximum limit of 5 hops and rejects redirects transitioning to foreign hostnames.

## 7. HOP-PATHBASE-001 (Subpath / PathBase Preservation)
- **Objective:** Verifies that applications hosted behind subpath prefixes (e.g. `/app`, `/api`) correctly maintain their `PathBase`.

---

**Author:** NulleN - Kadircan Üredi ([kadircanuredi.com.tr](https://kadircanuredi.com.tr) / hellonullen@gmail.com)
