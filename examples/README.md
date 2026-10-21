# Examples

This folder contains runnable examples for `qckdev.AspNetCore.Mvc.Filters.IpSafe`.

Each example focuses on a specific configuration style so you can start simple and evolve to more advanced scenarios.

## What You Will Find

- `IpSafeExample`: basic setup with separate IP safelist and forwarded-header configuration.
- `IpSafeExample.AppSettings`: modern setup using configuration sections.
- `IpSafeExample.Services`: modern setup using service-based providers.
- `IpSafeExample.DockerProxy`: reverse-proxy scenario with Nginx + anti-spoofing validation.

## Common Endpoints

Most examples expose these endpoints:

- `GET /WeatherForecast` -> protected with `IpSafeFilter`.
- `GET /WeatherForecastGlobal` -> protected at controller level.
- `GET /WeatherForecastGlobal/public` -> open endpoint via `AllowAnyIpAddress`.

## 1) IpSafeExample (Basic Setup)

### How It Is Implemented

- Uses `AddIpSafeFilter<IpSafeSettingsProvider>(options => options.WithConfiguration(Configuration, "Security:IpSafeList"))`.
- Configures forwarded headers directly with `UseForwardedHeaders(options => options.WithConfiguration(Configuration, "Security:ForwardedHeaders"))`.

### What It Demonstrates

- Separate allowed-client rules and trusted-proxy settings.
- Basic IP filtering with static settings.

### How To Configure

File: `IpSafeExample/appsettings.json`

- `Security:IpSafeList:IpAddresses` and `Security:IpSafeList:IpNetworks` define allowed clients.
- `Security:ForwardedHeaders` defines the processed headers, forwarding limit, and trusted proxies/networks. Set `KnownProxies` or `KnownNetworks` to match your reverse proxy; both are empty in the example configuration.

## 2) IpSafeExample.AppSettings (Modern, Config-Driven)

### How It Is Implemented

- Uses `AddIpSafeFilter<IpSafeSettingsProvider>(options => options.WithConfiguration(Configuration, "Security:IpSafeList"))`.
- Binds allowed IP rules from `Security:IpSafeList` through the registration overload.
- Uses `AddForwardedHeadersSettingsProvider<ForwardedHeadersSettingsProvider>(options => options.WithConfiguration(Configuration, "Security:ForwardedHeaders"))` for trusted proxy settings.
- Selects that standard provider with `UseForwardedHeaders(options => options.WithProvider<ForwardedHeadersSettingsProvider>())`.
- Does not call `UseIpSafeFilter(...)`, which would register a second forwarded-headers middleware.

### What It Demonstrates

- Separation of concerns:
  - `AddIpSafeFilter` -> allowed client IP rules.
  - `UseForwardedHeaders` -> trusted proxies/networks and `X-Forwarded-*` processing.
- Clean appsettings-driven setup.

### How To Configure

File: `IpSafeExample.AppSettings/appsettings.json`

- `Security:IpSafeList:IpAddresses`
- `Security:IpSafeList:IpNetworks`
- `Security:ForwardedHeaders:KnownProxies`
- `Security:ForwardedHeaders:KnownNetworks`

## 3) IpSafeExample.Services (Modern, Service-Driven)

### How It Is Implemented

- `AppSecuritySettingsService` implements both `IIpSafeSettingsProvider` and `IForwardedHeadersSettingsProvider`.
- Registers it with `AddIpSafeFilter<AppSecuritySettingsService>()` and `AddForwardedHeadersSettingsProvider<AppSecuritySettingsService>()`.
- Uses `UseForwardedHeaders(options => options.WithProvider<AppSecuritySettingsService>())` for proxy settings.

### What It Demonstrates

- Security settings loaded inside a service; registration and middleware are independent of the source.
- One service supplies allowed-client rules and trusted-proxy settings through separate interfaces.

### How To Configure

Implement the two provider interfaces in your service and register it in `Startup.ConfigureServices`. Both interfaces have a `GetSettingsAsync` method with different return types, so this example implements them explicitly.

`AppSecuritySettingsService` reads `Security:IpSafeList` and `Security:ForwardedHeaders` from `appsettings.json` through `IConfiguration`. The reads live inside the service. Replace them with a database query, Key Vault, or another source; the registration and middleware stay the same.

Edit those two sections in `IpSafeExample.Services/appsettings.json`. IpSafe rules are requested from the service for each protected request. Trusted-proxy settings are requested at startup; restart the API to apply changes to them.

## 4) IpSafeExample.DockerProxy (Nginx + API)

### How It Is Implemented

- Registers the standard provider using `options.WithConfiguration(Configuration, "Security:ForwardedHeaders")`.
- Selects it with `UseForwardedHeaders(options => options.WithProvider<ForwardedHeadersSettingsProvider>())`.
- Nginx container forwards requests to API and sets `X-Forwarded-For`.
- API trusts only the configured proxy IP/network.

### What It Demonstrates

- Real reverse-proxy flow.
- Public, `Office`, and `Partner` endpoints with expected responses `200`, `200`, and `403` through Nginx.
- Difference between:
  - trusted proxy request (accepted),
  - direct spoofed `X-Forwarded-For` request (rejected).

### How To Configure

- API settings: `IpSafeExample.DockerProxy/appsettings.json`
- Proxy settings: `IpSafeExample.DockerProxy/nginx/nginx.conf`
- Compose topology: `IpSafeExample.DockerProxy/docker-compose.yml`

Run `docker compose exec -T attacker sh /checks/verify.sh` to verify all nine status codes.

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
