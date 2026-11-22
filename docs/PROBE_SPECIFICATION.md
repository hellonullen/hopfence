# HopFence Probe Specification (Schema v1)

All HopFence adapters (`HopFence.AspNetCore`, `@hopfence/express`) implement the standard Schema Version 1 endpoint.

## Endpoint Details

- **Default Route:** `/_hopfence/probe`
- **Supported HTTP Method:** `GET`
- **Default Status:** Disabled (`404 Not Found`)
- **Activation:** `HOPFENCE_ENABLED=true` (or adapter options)
- **Authentication:** `Authorization: Bearer <HOPFENCE_TOKEN>`
- **Comparison:** Timing-attack resistant constant-time comparison (`FixedTimeEquals` / `timingSafeEqual`)
- **Caching:** `Cache-Control: no-store, no-cache, must-revalidate`
- **Data Protection:** No cookies, query parameters, bodies, or auth tokens are logged or returned.

## JSON Schema Example

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

**Author:** NulleN - Kadircan Üredi ([kadircanuredi.com.tr](https://kadircanuredi.com.tr) / hellonullen@gmail.com)
