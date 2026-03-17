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

## Quick Guide (Step by Step)

This section is incremental on purpose:
1. Start with one global list.
2. Add endpoint/controller attributes.
3. Add named schemes.
4. Add custom providers (optional).

If you are new to ASP.NET Core security, implement each step and verify before going to the next one.

## Step 1 - Basic Setup (single list)

`appsettings.json`

```json
{
  "IpSafeList": {
    "IpAddresses": "127.0.0.1;::1",
    "IpNetworks": "192.168.1.0/24;2001:db8::/64",
    "KnownProxies": "10.0.0.10;10.0.0.11"
  }
}
```

`Startup.cs`

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

public void ConfigureServices(IServiceCollection services)
{
  services.AddIpSafeFilter<IpSafeSettingsProvider>();
  services.Configure<IpSafeListSettings>(Configuration.GetSection("IpSafeList"));
  services.AddControllers();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
  app.UseIpSafeFilter();
  app.UseRouting();
  app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

## Step 2 - Protect endpoints with attributes

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

## Step 3 - Use named schemes (multiple IP profiles)

Named schemes let you define different IP allowlists and select them per endpoint.

### 3.1 Register schemes

```cs
services.AddIpSafeFilter<IpSafeSettingsProvider>();

// Default/global list (used by [IpSafeFilter] without parameters)
services.Configure<IpSafeListSettings>(Configuration.GetSection("IpSafeList"));

// Named scheme: Internal
services.AddIpSafeScheme("Internal", options =>
{
  Configuration.GetSection("IpSafeSchemes:Internal").Bind(options);
});

// Named scheme: Partner
services.AddIpSafeScheme("Partner", options =>
{
  options.IpAddresses = "198.51.100.20;198.51.100.21";
  options.IpNetworks = "198.51.100.0/24";
  options.KnownProxies = string.Empty;
});
```

`appsettings.json`

```json
{
  "IpSafeList": {
    "IpAddresses": "127.0.0.1;::1",
    "KnownProxies": "10.0.0.10"
  },
  "IpSafeSchemes": {
    "Internal": {
      "IpAddresses": "10.2.1.20;10.2.1.21",
      "IpNetworks": "10.2.1.0/24",
      "KnownProxies": ""
    },
    "Partner": {
      "IpAddresses": "198.51.100.20",
      "IpNetworks": "198.51.100.0/24",
      "KnownProxies": ""
    }
  }
}
```

### 3.2 Use schemes in attributes

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

## Step 4 - Custom providers

### 4.1 Custom provider (default profile only)

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
      IpNetworks = config.IpNetworks,
      KnownProxies = config.KnownProxies
    };
  }
}

services.AddIpSafeFilter<DatabaseIpSafeSettingsProvider>();
```

### 4.2 Custom provider with schemes

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
      IpNetworks = config.IpNetworks,
      KnownProxies = config.KnownProxies
    };
  }
}
```

## Backward Compatibility

Existing code continues working unchanged:
- `services.AddIpSafeFilter<IpSafeSettingsProvider>()`
- `[IpSafeFilter]` without parameters
- `IIpSafeSettingsProvider.GetSettingsAsync(...)`

Named schemes are additive:
- use `AddIpSafeScheme(...)`
- use `[IpSafeFilter("SchemeName")]`

## Security Checklist (important)

- Always configure trusted proxies (`KnownProxies`) correctly.
- Never trust `X-Forwarded-For` from arbitrary clients.
- Keep `UseIpSafeFilter()` before endpoint execution in pipeline.
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
