<a href="https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://img.shields.io/nuget/v/qckdev.AspNetCore.Mvc.Filters.IpSafe.svg" alt="NuGet Version"/></a>
<a href="https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=alert_status" alt="Quality Gate"/></a>
<a href="https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe"><img src="https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=coverage" alt="Code Coverage"/></a>
<a><img src="https://hfrances.visualstudio.com/Main/_apis/build/status/qckdev.AspNetCore.Mvc.Filters.IpSafe?branchName=master" alt="Azure Pipelines Status"/></a>

# qckdev.AspNetCore.Mvc.Filters.IpSafe

Provides a default set of tools for building an ASP.NET Core application.

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

    services.AddIpSafeFilter(ipSafeListSettings);
	services.AddControllers();
}

```
