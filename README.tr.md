# HopFence

> **Reverse Proxy Tanı ve Güvenlik Test Aracı**  
> *Geliştirici: **NulleN - Kadircan Üredi***  
> **Web Sitesi:** [kadircanuredi.com.tr](https://kadircanuredi.com.tr) &bull; **İletişim:** [hellonullen@gmail.com](mailto:hellonullen@gmail.com)

---

## Proje Hakkında

**HopFence**; reverse proxy veya yük dengeleyici (Nginx, Traefik, HAProxy, AWS ALB, Cloudflare vb.) arkasında çalışan web uygulamalarının proxy yapılandırmalarını denetleyen, yanlış yapılandırmaları ve güvenlik zafiyetlerini tespit eden açık kaynak bir test ve teşhis aracıdır.

HopFence kendisi bir reverse proxy değildir; uygulamanızın proxy arkasındaki gerçek istemci IP adresini, HTTPS protokolünü, Host bilgisini ve iletilen (forwarded) header değerlerini doğru ve güvenli şekilde işleyip işlemediğini dışarıdan ve içeriden kontrol eder.

---

## Test Başlıkları

| Kod | Test Adı | Açıklama |
|---|---|---|
| **HOP-HTTPS-001** | **HTTPS Tespiti** | Uygulamanın gelen isteği HTTPS olarak doğru algılayıp algılamadığını denetler. |
| **HOP-IP-001** | **İstemci IP Tespiti** | İstemci IP adresinin doğru çözümlenip çözümlenmediğini kontrol eder. |
| **HOP-SPOOF-XFF-001** | **X-Forwarded-For Koruması** | Sahte `X-Forwarded-For` göndererek IP spoofing açıklarını tespit eder. |
| **HOP-SPOOF-PROTO-001** | **Protokol Düşürme Koruması** | Sahte `X-Forwarded-Proto: http` göndererek protokol manipülasyonunu test eder. |
| **HOP-SPOOF-HOST-001** | **Host Header Zehirlenmesi** | Sahte `X-Forwarded-Host` ile Host Header Injection açıklarını kontrol eder. |
| **HOP-REDIR-001** | **Yönlendirme Denetimi** | HTTP -> HTTPS yönlendirmelerini, redirect döngülerini ve açık yönlendirmeleri inceler. |
| **HOP-PATHBASE-001** | **Alt Dizin Koruması** | Uygulamanın `/app` veya `/api` gibi alt dizinlerde PathBase korumasını test eder. |

---

## CLI Kullanımı

```bash
# Yardım menüsünü görüntüle
hopfence --help

# Örnek konfigürasyon dosyası oluştur (hopfence.yml)
hopfence init

# Tanı testlerini çalıştır
hopfence diagnose

# HTML veya JUnit formatında rapor üret
hopfence diagnose --format html --output rapor.html
hopfence diagnose --format junit --output sonuclar.xml
```

---

## Geliştirici ve İletişim

- **Geliştirici:** **NulleN - Kadircan Üredi**
- **Web:** [https://kadircanuredi.com.tr](https://kadircanuredi.com.tr)
- **E-Posta:** [hellonullen@gmail.com](mailto:hellonullen@gmail.com)
- **GitHub:** [https://github.com/hellonullen/hopfence](https://github.com/hellonullen/hopfence)
