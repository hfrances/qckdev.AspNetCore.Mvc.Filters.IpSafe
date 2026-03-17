# Examples

This folder contains runnable examples for `qckdev.AspNetCore.Mvc.Filters.IpSafe`.

Each example focuses on a specific configuration style so you can start simple and evolve to more advanced scenarios.

## What You Will Find

- `IpSafeExample`: legacy setup (backward compatibility path).
- `IpSafeExample.AppSettings`: modern setup using configuration sections.
- `IpSafeExample.Services`: modern setup using service-based providers.
- `IpSafeExample.DockerProxy`: reverse-proxy scenario with Nginx + anti-spoofing validation.

## Common Endpoints

Most examples expose these endpoints:

- `GET /WeatherForecast` -> protected with `IpSafeFilter`.
- `GET /WeatherForecastGlobal` -> protected at controller level.
- `GET /WeatherForecastGlobal/public` -> open endpoint via `AllowAnyIpAddress`.

## 1) IpSafeExample (Legacy)

### How It Is Implemented

- Uses legacy overload `AddIpSafeFilter(IpSafeListSettings)`.
- Calls `UseIpSafeFilter()` without additional builder configuration.

### What It Demonstrates

- Backward compatibility path.
- Basic IP filtering with static settings.

### How To Configure

File: `IpSafeExample/appsettings.json`

- `IpSafeList:IpAddresses` and `IpSafeList:IpNetworks` define allowed clients.
- `IpSafeList:KnownProxies` can be used for trusted proxies.

## 2) IpSafeExample.AppSettings (Modern, Config-Driven)

### How It Is Implemented

- Uses `AddIpSafeFilter<IpSafeSettingsProvider>()`.
- Binds allowed IP rules from `IpSafeList`.
- Configures trusted forwarded headers using:
  - `UseIpSafeFilter(cfg => cfg.WithConfiguration<IpSafeTrustedProxiesSettings>(...))`

### What It Demonstrates

- Separation of concerns:
  - `AddIpSafeFilter` -> allowed client IP rules.
  - `UseIpSafeFilter` -> trusted proxies/networks for `X-Forwarded-*`.
- Clean appsettings-driven setup.

### How To Configure

File: `IpSafeExample.AppSettings/appsettings.json`

- `IpSafeList:IpAddresses`
- `IpSafeList:IpNetworks`
- `IpSafeList:TrustedForwardedHeaders:KnownProxies`
- `IpSafeList:TrustedForwardedHeaders:KnownNetworks`

## 3) IpSafeExample.Services (Modern, Service-Driven)

### How It Is Implemented

- Uses `AddIpSafeFilter<ServiceIpSafeSettingsProvider>()` for allowed IP rules.
- Uses `UseIpSafeFilter(cfg => cfg.WithProxyService<ServiceTrustedProxiesProvider>())` for trusted proxies.
- Providers read values through `IAppSecuritySettingsService`.

### What It Demonstrates

- Dynamic configuration from services (for example DB/config service).
- Split provider model:
  - one provider for allowlist rules,
  - one provider for trusted proxies/networks.

### How To Configure

File: `IpSafeExample.Services/appsettings.json`

- `IpSafeList:IpAddresses`
- `IpSafeList:IpNetworks`
- `IpSafeList:TrustedKnownProxies`
- `IpSafeList:TrustedKnownNetworks`

Note: service-based trusted proxy values are resolved at startup. If values change later, restart the API.

## 4) IpSafeExample.DockerProxy (Nginx + API)

### How It Is Implemented

- ASP.NET Core API configured with `WithConfiguration<IpSafeTrustedProxiesSettings>(...)`.
- Nginx container forwards requests to API and sets `X-Forwarded-For`.
- API trusts only the configured proxy IP/network.

### What It Demonstrates

- Real reverse-proxy flow.
- Difference between:
  - trusted proxy request (accepted),
  - direct spoofed `X-Forwarded-For` request (rejected).

### How To Configure

- API settings: `IpSafeExample.DockerProxy/appsettings.json`
- Proxy settings: `IpSafeExample.DockerProxy/nginx/nginx.conf`
- Compose topology: `IpSafeExample.DockerProxy/docker-compose.yml`

Detailed run and validation commands are documented in:

- `IpSafeExample.DockerProxy/README.md`

## Running Local Examples

From each example folder:

```bash
dotnet run -f net8.0
```

For Docker proxy example:

```bash
docker compose up --build -d
```

## Suggested Learning Order

1. `IpSafeExample`
2. `IpSafeExample.AppSettings`
3. `IpSafeExample.Services`
4. `IpSafeExample.DockerProxy`
