# @hopfence/express

> Reverse proxy diagnostics & security testing probe adapter for Express.js applications.
>
> **Author:** NulleN - Kadircan Üredi ([kadircanuredi.com.tr](https://kadircanuredi.com.tr) / hellonullen@gmail.com)

`@hopfence/express` provides a secure, lightweight diagnostic probe endpoint (`/_hopfence/probe`) for applications running behind reverse proxies (Nginx, Traefik, HAProxy, AWS ALB, Cloudflare). It works seamlessly with the `hopfence` CLI to test and audit proxy header forwarding, IP spoofing protections, and protocol detection.

---

## 📦 Installation

```bash
npm install @hopfence/express
```

---

## 🚀 Quick Start

```javascript
const express = require('express');
const hopFence = require('@hopfence/express');

const app = express();

// Set your trusted proxy configuration (e.g. 1 hop or loopback)
app.set('trust proxy', ['loopback']);

// Register HopFence probe middleware
app.use(hopFence({
  enabled: process.env.HOPFENCE_ENABLED === 'true',
  token: process.env.HOPFENCE_TOKEN
}));

app.get('/', (req, res) => {
  res.send('Application is running behind proxy.');
});

app.listen(3000, () => console.log('Server listening on port 3000'));
```

---

## 🔒 Security Best Practices

1. **Disabled by default**: The probe returns `404 Not Found` unless explicitly enabled via `enabled: true` or `HOPFENCE_ENABLED=true`.
2. **Bearer Token Authentication**: The probe requires `Authorization: Bearer <token>` verified via `crypto.timingSafeEqual` (timing attack safe).
3. **Zero Sensitive Data Leakage**: Cookies, query parameters, request bodies, and authentication tokens are never captured or returned in the probe response.
4. **No Cache**: Responses set `Cache-Control: no-store, no-cache, must-revalidate`.

---

## 📄 License

MIT © [NulleN - Kadircan Üredi](https://kadircanuredi.com.tr)
