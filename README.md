<a href="https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://img.shields.io/nuget/v/qckdev.AspNetCore.Mvc.Filters.IpSafe.svg" alt="NuGet Version"/></a>
<a href="https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=alert_status" alt="Quality Gate"/></a>
<a href="https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=coverage" alt="Code Coverage"/></a>
<a><img src="https://hfrances.visualstudio.com/qckdev/_apis/build/status/qckdev.AspNetCore.Mvc.Filters.IpSafe?branchName=master" alt="Azure Pipelines Status"/></a>

# qckdev.AspNetCore.Mvc.Filters.IpSafe

Provides a solution to grant/deny access to some IP ranges.

```json
{
  (...)
  
  "IpSafeList": {
    "IpAddresses": "127.0.0.1;::1",
    "IpNetworks": "192.168.1.0/24;2001:0db8::1/64;110.40.88.12/28"
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
  var ipSafeListSettings = Configuration.GetSection("IpSafeList").Get<IpSafeListSettings>();

  (...)
  services.AddIpSafeFilter(ipSafeListSettings);
  (...)
  services.AddControllers();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
  (...)
  app.UseRouting();
  app.UseIpSafeFilter(options =>
  {
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
  });
  (...)
}
```

```cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
  (...)

  [HttpGet, IpSafeFilter]
  public IEnumerable<WeatherForecast> Get()
  {
    (...)
  }
}
```

Nginx configuration

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