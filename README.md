<a href="https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://img.shields.io/nuget/v/qckdev.AspNetCore.Mvc.Filters.IpSafe.svg" alt="NuGet Version"/></a>
<a href="https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=alert_status" alt="Quality Gate"/></a>
<a href="https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=coverage" alt="Code Coverage"/></a>
<a><img src="https://hfrances.visualstudio.com/qckdev/_apis/build/status/qckdev.AspNetCore.Mvc.Filters.IpSafe?branchName=master" alt="Azure Pipelines Status"/></a>

# qckdev.AspNetCore.Mvc.Filters.IpSafe

Provides a solution to grant/deny access to some IP ranges with extensible configuration strategies.

## Quick Start

```json
{
  (...),
  "IpSafeList": {
    "IpAddresses": "127.0.0.1;::1",
    "IpNetworks": "192.168.1.0/24;2001:0db8::1/64;110.40.88.12/28",
    "KnownProxies": "proxy.example.com" // SAFE - only trusts specific proxies
  }
}
```

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

public void ConfigureServices(IServiceCollection services)
{
  services.AddIpSafeFilter<IpSafeSettingsProvider>();
  services.Configure<IpSafeListSettings>(Configuration.GetSection("IpSafeList"));
  services.AddControllers();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
  (...)
  app.UseIpSafeFilter();
  app.UseRouting();
  (...)
}
```

## Custom Settings Provider

Implement `IIpSafeSettingsProvider` for custom configuration sources:

```cs
public class DatabaseIpSafeSettingsProvider : IIpSafeSettingsProvider
{
  private readonly IIpSecurityRepository _repository;
  
  public async Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken)
  {
    var config = await _repository.GetCurrentConfigAsync(cancellationToken);
    return config != null ? new IpSafeListSettings 
    { 
      IpAddresses = config.IpAddresses,
      IpNetworks = config.IpNetworks,
      KnownProxies = config.KnownProxies
    } : null;
  }
}

services.AddIpSafeFilter<DatabaseIpSafeSettingsProvider>();
```

## Usage

```cs
[ApiController, Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
  [HttpGet, IpSafeFilter]
  public IEnumerable<WeatherForecast> Get() => (...);
  
  [HttpGet("public"), AllowAnyIpAddress]
  public IEnumerable<WeatherForecast> GetPublic() => (...);
}
```

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

This library includes comprehensive integration tests covering IP filtering, forwarded headers, and attribute overrides.

**3 integration tests** validate IP-based access control:
- Loopback address handling
- X-Forwarded-For header validation
- Per-endpoint `[AllowAnyIpAddress]` override

For detailed testing documentation, see [Integration Testing Guide](docs/TESTING.md).
