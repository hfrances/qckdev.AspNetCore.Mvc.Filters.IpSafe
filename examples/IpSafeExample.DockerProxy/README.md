# IpSafeExample.DockerProxy

This example runs `IpSafe` behind an Nginx reverse proxy using Docker Compose.

It demonstrates:
- trusted proxy configuration (`KnownProxies`)
- forwarded client IP processing (`X-Forwarded-For`)
- spoofing protection when requests do not come from a trusted proxy

## Topology

- `ipsafe-nginx` (`172.28.0.10`) -> trusted proxy
- `ipsafe-api` (`172.28.0.20`) -> ASP.NET Core API protected by `IpSafeFilter`
- `ipsafe-attacker` (`172.28.0.30`) -> helper container to simulate spoofing

## Configuration Highlights

`appsettings.json`:
- allowed client IP: `198.51.100.10`
- trusted proxy: `172.28.0.10`

The API uses:

```csharp
app.UseIpSafeFilter(cfg =>
{
  cfg.WithConfiguration<IpSafeTrustedProxiesSettings>(Configuration, (config, target) =>
    config.GetSection("IpSafeList:TrustedForwardedHeaders").Bind(target));
});
```

## Run

From this folder:

```bash
docker compose up --build -d
```

## Validate

### 1. Through trusted proxy (expected `200`)

```bash
curl -i http://localhost:8080/WeatherForecast
```

Why it works:
- request comes through trusted Nginx (`172.28.0.10`)
- Nginx forwards `X-Forwarded-For: 198.51.100.10`
- `198.51.100.10` is allowed in `IpSafeList.IpAddresses`

### 2. Direct call to API with spoofed header (expected `403`)

```bash
curl -i -H "X-Forwarded-For: 198.51.100.10" http://localhost:8081/WeatherForecast
```

Why it is blocked:
- direct caller is not the trusted proxy (`KnownProxies`)
- spoofed `X-Forwarded-For` is ignored
- real remote IP is not in allowed list

### 3. Spoofing from another container (expected `403`)

```bash
docker compose exec attacker curl -i -H "X-Forwarded-For: 198.51.100.10" http://api:8080/WeatherForecast
```

## Stop

```bash
docker compose down
```

## Notes

- `UseIpSafeFilter()` defaults include `ForwardLimit = 1` and `X-Forwarded-For | X-Forwarded-Proto`.
- If you change trusted proxies/networks at runtime, restart the API container.
