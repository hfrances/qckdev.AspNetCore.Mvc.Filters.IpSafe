[![NuGet Version](https://img.shields.io/nuget/v/qckdev.AspNetCore.Mvc.Filters.IpSafe.svg)](https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=alert_status)](https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe)
[![Code Coverage](https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=coverage)](https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe)
![Azure Pipelines Status](https://hfrances.visualstudio.com/qckdev/_apis/build/status/qckdev.AspNetCore.Mvc.Filters.IpSafe?branchName=master)

# qckdev.AspNetCore.Mvc.Filters.IpSafe

Provides a solution to grant/deny access to some IP ranges with extensible configuration strategies.

## 📦 Packages

- `qckdev.AspNetCore.Mvc.Filters.IpSafe`: IP filtering attributes, settings providers, and middleware integration.
- `qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger`: Swagger/OpenAPI integration for visual IP-safe endpoint markers.

## 🛠️ Installation

```bash
dotnet add package qckdev.AspNetCore.Mvc.Filters.IpSafe
```

## ⚡ Quick Start

This section is incremental on purpose:
1. Start with one global list.
2. Configure trusted proxies/networks for forwarded headers.
3. Add endpoint/controller attributes.
4. Add named schemes.
5. Add custom providers (optional).

If you are new to ASP.NET Core security, implement each step and verify before going to the next one.

## Before You Start

`IpSafe` uses two different concepts:

- Allowed client IPs/networks: who is allowed to call protected endpoints (`IpAddresses`, `IpNetworks`).
- Trusted proxies/networks: which reverse proxies are allowed to provide `X-Forwarded-*` (`KnownProxies`, `KnownNetworks`).

If your API is behind Nginx, Azure Front Door, API Gateway, Traefik, etc., configure trusted proxies/networks explicitly.

### `KnownProxies` vs `KnownNetworks` (trusted forwarded headers)

- `KnownProxies`: exact proxy IPs you trust to send `X-Forwarded-*` headers (for example one or more fixed reverse-proxy IPs).
- `KnownNetworks`: trusted proxy CIDR ranges (for example internal proxy subnet or load balancer network).

These settings **do not define who can access your endpoint**.  
They define **from which proxy origins forwarded headers are trusted**.

Use them together with `IpAddresses`/`IpNetworks` (allowed clients).

| Scenario | `KnownProxies` | `KnownNetworks` | Recommended value |
|---|---|---|---|
| No proxy | Optional | Optional | Leave both empty. `IpSafe` will evaluate direct `RemoteIpAddress`. |
| With fixed proxy IPs | Required | Optional | Set trusted proxy IPs in `KnownProxies` (for example `10.0.0.10;10.0.0.11`). |
| With proxy subnet/range | Optional | Required | Set trusted CIDR ranges in `KnownNetworks` (for example `10.0.0.0/24`). |
| Hybrid (some fixed proxies + subnet) | Required | Required | Fill both: fixed proxies in `KnownProxies` and dynamic proxy ranges in `KnownNetworks`. |

> Default forwarded headers behavior in `UseIpSafeFilter()`:
> - `ForwardedHeaders = XForwardedFor | XForwardedProto`
> - `ForwardLimit = 1`
>
> You only need `WithForwardedHeaders(...)` when you want to override these defaults.
## Step 1 - Basic Setup (single list)

Use this first if you want a minimal working setup. If your API is not behind a reverse proxy, omit `KnownProxies` and `KnownNetworks` from `Security:ForwardedHeaders`.

`appsettings.json`

Allowed-client lists (`IpAddresses`, `IpNetworks`) and trusted-proxy lists (`KnownProxies`, `KnownNetworks`) accept either a JSON array or a semicolon-delimited string. Keep the two concerns in sibling sections under `Security`, and use one format consistently in each example.

Array form:

```json
{
  "Security": {
    "ForwardedHeaders": {
      "ForwardedHeaders": "XForwardedFor, XForwardedProto",
      "ForwardLimit": 1,
      "KnownProxies": [ "10.0.0.10" ]
    },
    "IpSafeList": {
      "IpAddresses": [ "203.0.113.25" ],
      "IpNetworks": [ "203.0.113.0/24" ]
    }
  }
}
```

Semicolon-delimited string form:

```json
{
  "Security": {
    "ForwardedHeaders": {
      "ForwardedHeaders": "XForwardedFor, XForwardedProto",
      "ForwardLimit": 1,
      "KnownProxies": "10.0.0.10"
    },
    "IpSafeList": {
      "IpAddresses": "203.0.113.25",
      "IpNetworks": "203.0.113.0/24"
    }
  }
}
```

`Startup.cs`

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using qckdev.AspNetCore.ForwardedHeaders;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

public void ConfigureServices(IServiceCollection services)
{
  services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
      options.WithConfiguration(Configuration, "Security:IpSafeList"));
  services.AddForwardedHeadersSettingsProvider<ForwardedHeadersSettingsProvider>(options =>
      options.WithConfiguration(Configuration, "Security:ForwardedHeaders"));
  services.AddControllers();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
  app.UseForwardedHeaders(options =>
    options.WithProvider<ForwardedHeadersSettingsProvider>());
  app.UseRouting();
  app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

Configure values in code with `Configure`:

```cs
services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
  options.Configure(settings =>
  {
    settings.IpAddresses = "127.0.0.1;::1";
    settings.IpNetworks = null;
  }));
```

## Step 2 - Trusted Proxies/Networks for Forwarded Headers

When using `qckdev.AspNetCore.ForwardedHeaders` together with IpSafe, configure trusted proxies/networks in the sibling `Security:ForwardedHeaders` section and register only `UseForwardedHeaders(...)`. Do not also call `UseIpSafeFilter(...)`, because that method registers its own forwarded-headers middleware.

### Option A - From `appsettings.json` via the standard provider

`appsettings.json`

```json
{
  "Security": {
    "ForwardedHeaders": {
      "ForwardedHeaders": "XForwardedFor, XForwardedProto",
      "ForwardLimit": 1,
      "KnownProxies": [ "127.0.0.1", "::1" ],
      "KnownNetworks": [ "10.0.0.0/8" ]
    },
    "IpSafeList": {
      "IpAddresses": [ "127.0.0.1", "::1" ],
      "IpNetworks": [ "192.168.1.0/24" ]
    }
  }
}
```

`Startup.cs`

```cs
services.AddForwardedHeadersSettingsProvider<ForwardedHeadersSettingsProvider>(options =>
    options.WithConfiguration(Configuration, "Security:ForwardedHeaders"));

app.UseForwardedHeaders(options =>
  options.WithProvider<ForwardedHeadersSettingsProvider>());
```

Use `options.Configure(settings => ...)` to configure forwarded headers in code. See the [ForwardedHeaders registration alternatives](../qckdev.AspNetCore.ForwardedHeaders/README.md#registration-alternatives) for examples.

### Option B - From service via `WithProvider`

Use a separate forwarded-headers provider when proxy settings come from a service. The provider is queried once at startup; restart the API to apply changes.

```cs
using qckdev.AspNetCore.ForwardedHeaders;

services.AddForwardedHeadersSettingsProvider<MyForwardedHeadersProvider>();

app.UseForwardedHeaders(options =>
  options.WithProvider<MyForwardedHeadersProvider>());

public sealed class MyForwardedHeadersProvider : IForwardedHeadersSettingsProvider
{
  public Task<ForwardedHeadersSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult<ForwardedHeadersSettings?>(new ForwardedHeadersSettings
    {
      ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
      ForwardLimit = 1,
      KnownProxies = "10.0.0.10",
      KnownNetworks = ""
    });
  }
}
```

`IpSafeList` and its providers contain only allowed-client rules. Keep proxy trust in `ForwardedHeaders` and its own provider.

`UseIpSafeFilter(...)` remains available for legacy compatibility. New setups use the separate middleware above and do not register both.

## Step 3 - Protect endpoints with attributes

```cs
using Microsoft.AspNetCore.Mvc;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

[ApiController]
[Route("weather")]
[IpSafeFilter] // Controller-level protection
public class WeatherController : ControllerBase
{
  [HttpGet("secure")]
  public IActionResult Secure() => Ok("protected");

  [HttpGet("public")]
  [AllowAnyIpAddress] // Endpoint-level override
  public IActionResult Public() => Ok("public");
}
```

### Attribute precedence rules

- Endpoint level has priority over controller level.
- At the same level, `AllowAnyIpAddress` has priority over `IpSafeFilter`.

Examples:
- Controller `[IpSafeFilter]` + endpoint `[AllowAnyIpAddress]` => allows any.
- Controller `[AllowAnyIpAddress]` + endpoint `[IpSafeFilter]` => checks IP.

## Step 4 - Use named schemes (multiple IP profiles)

Named schemes let you define different IP allowlists and select them per endpoint.
Use this when one API exposes endpoints for different consumers (internal, partners, operations, etc.).

### 4.1 Register schemes

```cs
// Default/global list (used by [IpSafeFilter] without parameters)
services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
    options.WithConfiguration(Configuration, "Security:IpSafeList"));

// Named scheme: Internal
services.AddIpSafeFilter("Internal", options =>
  options.WithConfiguration(Configuration, "IpSafeSchemes:Internal"));

// Named scheme: Partner
services.AddIpSafeFilter("Partner", options =>
    options.Configure(settings =>
    {
      settings.IpAddresses = "198.51.100.20;198.51.100.21";
      settings.IpNetworks = "198.51.100.0/24";
    }));
```

`appsettings.json`

```json
{
  "Security": {
    "IpSafeList": {
      "IpAddresses": "127.0.0.1;::1"
    }
  },
  "IpSafeSchemes": {
    "Internal": {
      "IpAddresses": "10.2.1.20;10.2.1.21",
      "IpNetworks": "10.2.1.0/24"
    },
    "Partner": {
      "IpAddresses": "198.51.100.20",
      "IpNetworks": "198.51.100.0/24"
    }
  }
}
```

### 4.2 Use schemes in attributes

```cs
[ApiController]
[Route("secure")]
public class SecureController : ControllerBase
{
  [HttpGet("default")]
  [IpSafeFilter] // Uses default IpSafeList
  public IActionResult DefaultProfile() => Ok();

  [HttpGet("internal")]
  [IpSafeFilter("Internal")] // Uses only Internal scheme
  public IActionResult InternalProfile() => Ok();

  [HttpGet("internal-or-partner")]
  [IpSafeFilter("Internal", "Partner")] // OR semantics: match any configured scheme
  public IActionResult InternalOrPartner() => Ok();
}
```

## Step 5 - Custom providers

Use IpSafe providers for allowed-client rules from dynamic sources (database, external services, centralized configuration). Configure proxy trust separately through IForwardedHeadersSettingsProvider; those settings are resolved once at startup.

### 5.1 Custom provider (default profile only)

Implement `IIpSafeSettingsProvider` when you only need the default profile.

```cs
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

public class DatabaseIpSafeSettingsProvider : IIpSafeSettingsProvider
{
  private readonly IIpSecurityRepository _repository;

  public DatabaseIpSafeSettingsProvider(IIpSecurityRepository repository)
  {
    _repository = repository;
  }

  public async Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
  {
    var config = await _repository.GetCurrentConfigAsync(cancellationToken);
    if (config == null)
    {
      return null;
    }

    return new IpSafeListSettings
    {
      IpAddresses = config.IpAddresses,
      IpNetworks = config.IpNetworks
    };
  }
}

services.AddIpSafeFilter<DatabaseIpSafeSettingsProvider>();
```

### 5.2 Custom provider with schemes

Implement `IIpSafeSchemeSettingsProvider` to resolve settings by scheme name.

```cs
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

public class DatabaseIpSafeSchemeSettingsProvider : IIpSafeSchemeSettingsProvider
{
  private readonly IIpSecurityRepository _repository;

  public DatabaseIpSafeSchemeSettingsProvider(IIpSecurityRepository repository)
  {
    _repository = repository;
  }

  public Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
  {
    // Backward-compatible default profile
    return GetSettingsAsync("Default", cancellationToken);
  }

  public async Task<IpSafeListSettings?> GetSettingsAsync(string scheme, CancellationToken cancellationToken = default)
  {
    var config = await _repository.GetBySchemeAsync(scheme, cancellationToken);
    if (config == null)
    {
      return null;
    }

    return new IpSafeListSettings
    {
      IpAddresses = config.IpAddresses,
      IpNetworks = config.IpNetworks
    };
  }
}
```

Registration:

```cs
services.AddIpSafeFilter<DatabaseIpSafeSchemeSettingsProvider>();
```

## Recommended Validation Flow

After each step, validate with real requests:

1. Call protected endpoint from allowed IP and verify `200`.
2. Call protected endpoint from non-allowed IP and verify `403`.
3. If behind proxy, test trusted and untrusted proxy scenarios (`X-Forwarded-For` spoofing checks).
4. If using schemes, test each scheme endpoint independently.

## Backward Compatibility

Provider-only registration and filter attributes remain available:
- `services.AddIpSafeFilter<IpSafeSettingsProvider>()`
- `[IpSafeFilter]` without parameters
- `IIpSafeSettingsProvider.GetSettingsAsync(...)`

Callbacks receive an options builder. Use `WithConfiguration(Configuration, sectionName)` to load a section or `Configure(settings => ...)` to assign values. The `(Configuration, sectionName)` registration overload remains available.

Named schemes are additive:
- use `AddIpSafeFilter("SchemeName", ...)`
- use `[IpSafeFilter("SchemeName")]`

## Security Checklist (important)

- Always configure trusted proxies (`KnownProxies`) correctly.
- Never trust `X-Forwarded-For` from arbitrary clients.
- Run `UseForwardedHeaders(...)` before middleware that reads the client IP or request scheme.
- Add tests for spoofing scenarios (trusted vs untrusted proxy).

## Nginx Configuration

```nginx
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;

real_ip_header X-Forwarded-For;
#set_real_ip_from 0.0.0.0/0;
set_real_ip_from 10.0.0.0/8;
set_real_ip_from 172.16.0.0/12;
set_real_ip_from 192.168.0.0/16;
real_ip_recursive on;
```

## Private Address Ranges

|  Class  |      Mask      |      From       |       To        |
|:-------:|:--------------:|:---------------:|:---------------:|
| Class A |   10.0.0.0/8   |     10.0.0.0    |  10.255.255.255 |
| Class B |  172.16.0.0/12 |    172.16.0.0   |  172.31.255.255 |
| Class C | 192.168.0.0/16 |   192.168.0.0   | 192.168.255.255 |

## Testing

This library includes integration and unit tests covering:
- basic IP filtering
- attribute precedence (controller vs endpoint)
- proxy trust and `X-Forwarded-For` spoofing protection
- named-scheme resolution and multi-scheme behavior

For detailed testing documentation, see [Integration Testing Guide](docs/TESTING.md).

## 🤝 Contributing
Issues and pull requests are welcome! See the contribution guidelines (coming soon).

## 📜 License
This project is licensed under the terms of the [MIT License](LICENSE).
