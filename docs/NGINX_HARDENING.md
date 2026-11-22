# Nginx Hardening Guide for HopFence Compliance

To ensure your Nginx reverse proxy passes all HopFence security audits, follow these guidelines:

## Secure Server Block Configuration

```nginx
server {
    listen 443 ssl http2;
    server_name staging.example.com;

    # SSL certificates
    ssl_certificate /etc/ssl/certs/fullchain.pem;
    ssl_certificate_key /etc/ssl/private/privkey.pem;

    location / {
        proxy_pass http://backend_upstream;
        proxy_http_version 1.1;

        # 1. Sanitize and forward real client IP
        # If upstream is the only proxy:
        proxy_set_header X-Forwarded-For $remote_addr;
        # Or if behind a multi-proxy CDN (e.g. Cloudflare):
        # proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

        # 2. Prevent proto downgrade by forcing actual scheme
        proxy_set_header X-Forwarded-Proto $scheme;

        # 3. Prevent Host poisoning
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header Host $host;

        # Connection management
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
    }
}
```

---

**Author:** NulleN - Kadircan Üredi ([kadircanuredi.com.tr](https://kadircanuredi.com.tr) / hellonullen@gmail.com)
