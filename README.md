[![NuGet Version](https://img.shields.io/nuget/v/qckdev.AspNetCore.Mvc.Filters.IpSafe.svg)](https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=alert_status)](https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe)
[![Code Coverage](https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe&metric=coverage)](https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe)
![Azure Pipelines Status](https://dev.azure.com/hfrances/qckdev/_apis/build/status/qckdev.AspNetCore.Mvc.Filters.IpSafe?branchName=master)

# qckdev.AspNetCore.Mvc.Filters.IpSafe

IP allowlists for ASP.NET Core MVC controllers and actions. Protect endpoints with attributes, select different lists with named schemes, and load rules from configuration or your own services.

## 📦 Packages

- `qckdev.AspNetCore.Mvc.Filters.IpSafe`: IP filtering attributes and settings providers.
- `qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger`: optional OpenAPI metadata and IP badges in Swagger UI. See [Show protection in Swagger](#show-protection-in-swagger).

## 🛠️ Installation

~~~bash
dotnet add package qckdev.AspNetCore.Mvc.Filters.IpSafe
~~~

## ⚡ Quick Start

### Protect your first endpoint

This example allows local requests to a protected controller action. Start without a reverse proxy; if you use one, also follow [Run behind a reverse proxy](#run-behind-a-reverse-proxy).

Add the allowed client addresses to `appsettings.json`:

~~~json
{
  "Security": {
    "IpSafeList": {
      "IpAddresses": [ "127.0.0.1", "::1" ]
    }
  }
}
~~~

Register the provider in `Program.cs`:

~~~csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
    options.WithConfiguration(builder.Configuration, "Security:IpSafeList"));

var app = builder.Build();

app.MapControllers();
app.Run();
~~~

Add a controller:

~~~csharp
using Microsoft.AspNetCore.Mvc;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

[ApiController]
[Route("network")]
public class NetworkController : ControllerBase
{
    [HttpGet("protected")]
    [IpSafeFilter]
    public IActionResult Protected() => Ok("allowed");

    [HttpGet("public")]
    public IActionResult Public() => Ok("public");
}
~~~

A local request to `/network/protected` reaches the action. A client outside the configured list receives `403 Forbidden`. The public action has no IP restriction.

`AddIpSafeFilter` registers the settings provider; it does not protect every endpoint. Apply `[IpSafeFilter]` to each action or controller you want to protect. Its parameterless form uses the default list.

**An empty or missing list means no IP restrictions, even on an action with `[IpSafeFilter]`.** See [Cybersecurity](#cybersecurity) before deploying.

### Configure allowed clients

Use `IpAddresses` for individual client addresses and `IpNetworks` for CIDR ranges. A client is allowed if its IP matches either list. For example, the following `Security:IpSafeList` section allows local requests and clients in the example subnet:

~~~json
{
  "IpAddresses": [ "127.0.0.1", "::1" ],
  "IpNetworks": [ "192.0.2.0/24" ]
}
~~~

Both properties also accept semicolon-separated strings:

~~~json
{
  "IpAddresses": "127.0.0.1;::1",
  "IpNetworks": "192.0.2.0/24"
}
~~~

Use your clients' actual addresses and networks. Setting either property to `null`, `""`, or `[]` adds no rules for that property. If both contain no rules, IP filtering is skipped.

### Apply attributes to controllers and actions

Apply `[IpSafeFilter]` to a controller to protect its actions, then use `[AllowAnyIpAddress]` for public exceptions:

~~~csharp
[ApiController]
[Route("weather")]
[IpSafeFilter]
public class WeatherController : ControllerBase
{
    [HttpGet("secure")]
    public IActionResult Secure() => Ok("protected");

    [HttpGet("public")]
    [AllowAnyIpAddress]
    public IActionResult Public() => Ok("public");
}
~~~

An action's attributes take precedence over its controller's attributes. At the same level, `[AllowAnyIpAddress]` takes precedence over `[IpSafeFilter]`.

| Controller | Action | IP filtering |
| --- | --- | --- |
| `[IpSafeFilter]` | No IP attribute | Uses the controller's list or schemes |
| `[IpSafeFilter]` | `[AllowAnyIpAddress]` | Skipped |
| `[AllowAnyIpAddress]` | `[IpSafeFilter]` | Uses the action's list or schemes |
| `[IpSafeFilter("Internal")]` | `[IpSafeFilter("Partner")]` | Uses Partner only |

### Use named schemes

Named schemes let different endpoints use different allowlists, such as Internal and Partner. Add these sections to `appsettings.json`:

~~~json
{
  "Security": {
    "IpSafeList": {
      "IpAddresses": [ "127.0.0.1", "::1" ]
    },
    "IpSafeSchemes": {
      "Internal": {
        "IpNetworks": [ "10.2.1.0/24" ]
      },
      "Partner": {
        "IpAddresses": [ "198.51.100.20", "198.51.100.21" ]
      }
    }
  }
}
~~~

Replace the original provider registration with this chain to keep the default list and add the named settings:

~~~csharp
builder.Services
    .AddIpSafeFilter<IpSafeSettingsProvider>(options =>
        options.WithConfiguration(builder.Configuration, "Security:IpSafeList"))
    .AddIpSafeFilter("Internal", options =>
        options.WithConfiguration(builder.Configuration, "Security:IpSafeSchemes:Internal"))
    .AddIpSafeFilter("Partner", options =>
        options.WithConfiguration(builder.Configuration, "Security:IpSafeSchemes:Partner"));
~~~

The generic registration supplies the provider used by the attributes. The named registrations configure additional lists for that provider.

Select the schemes on controller actions:

~~~csharp
[HttpGet("default")]
[IpSafeFilter]
public IActionResult DefaultProfile() => Ok();

[HttpGet("internal")]
[IpSafeFilter("Internal")]
public IActionResult InternalProfile() => Ok();

[HttpGet("internal-or-partner")]
[IpSafeFilter("Internal", "Partner")]
public IActionResult InternalOrPartner() => Ok();
~~~

Multiple schemes combine their rules with OR semantics: an IP matching any selected scheme is allowed. Specifying a scheme does not also apply the default list.

With the standard provider, if none of the selected schemes contains rules, the request has no IP restrictions. Check scheme names and configuration before publishing.

### Configure values in code

Use `Configure` instead of `WithConfiguration` when the allowed addresses are held in code:

~~~csharp
builder.Services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
    options.Configure(settings =>
    {
        settings.IpAddresses = "127.0.0.1;::1";
        settings.IpNetworks = "192.0.2.0/24";
    }));
~~~

The same callback is available for a named scheme:

~~~csharp
builder.Services.AddIpSafeFilter("Partner", options =>
    options.Configure(settings =>
    {
        settings.IpAddresses = "198.51.100.20;198.51.100.21";
    }));
~~~

### Load settings from a service

A service implementing `IIpSafeSettingsProvider` can supply the default list from a database, Key Vault, another API, or configuration.

This working example reads configuration inside the service. Replace the read with a call to your own source as needed:

~~~csharp
using Microsoft.Extensions.Configuration;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System.Threading;
using System.Threading.Tasks;

public sealed class AppSecuritySettingsService : IIpSafeSettingsProvider
{
    private readonly IConfiguration _configuration;

    public AppSecuritySettingsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<IpSafeListSettings?> GetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IpSafeListSettings?>(
            _configuration.GetSection("Security:IpSafeList")
                .Get<IpSafeListSettings>());
    }
}
~~~

Replace the standard provider registration with:

~~~csharp
builder.Services.AddIpSafeFilter<AppSecuritySettingsService>();
~~~

IpSafe queries the provider when validating a protected request. If the source is remote or expensive to query, handle caching in your service.

To support named schemes, change the service to implement `IIpSafeSchemeSettingsProvider`, keep its default-list method, and add this overload:

~~~csharp
public Task<IpSafeListSettings?> GetSettingsAsync(
    string scheme,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    return Task.FromResult<IpSafeListSettings?>(
        _configuration.GetSection("Security:IpSafeSchemes:" + scheme)
            .Get<IpSafeListSettings>());
}
~~~

A provider implementing only `IIpSafeSettingsProvider` uses its default list even when an attribute specifies scheme names. Implement `IIpSafeSchemeSettingsProvider` when the selected names must determine the rules.

See [IpSafeExample.Services](examples/IpSafeExample.Services) for a complete service-based application.

### Run behind a reverse proxy

IpSafe checks `HttpContext.Connection.RemoteIpAddress`. Behind a reverse proxy, use `qckdev.AspNetCore.ForwardedHeaders` to restore the client IP before controller actions run.

Keep the two concerns in separate configuration sections:

- `Security:IpSafeList` and `Security:IpSafeSchemes` identify allowed clients.
- `Security:ForwardedHeaders` identifies proxies allowed to supply forwarded headers.

Add the proxy settings alongside the existing IpSafe sections:

~~~json
{
  "Security": {
    "ForwardedHeaders": {
      "KnownProxies": [ "10.0.0.10" ]
    }
  }
}
~~~

Use the actual address of your proxy. The forwarded-headers library processes `XForwardedFor` and `XForwardedProto` by default, with `ForwardLimit = 1`.

Add this service registration:

~~~csharp
using qckdev.AspNetCore.ForwardedHeaders;

builder.Services.AddForwardedHeadersSettingsProvider<ForwardedHeadersSettingsProvider>(options =>
    options.WithConfiguration(builder.Configuration, "Security:ForwardedHeaders"));
~~~

Then add the middleware before routing, authentication, or other middleware that uses the client IP or scheme:

~~~csharp
app.UseForwardedHeaders(options =>
    options.WithProvider<ForwardedHeadersSettingsProvider>());
~~~

Proxy settings from this provider are read at application startup. This differs from the IpSafe provider, which supplies client rules during request validation.

See the [ForwardedHeaders README](https://github.com/hfrances/qckdev.AspNetCore.ForwardedHeaders) for trusted networks, custom proxy-settings providers, and allowed hosts. Its [Nginx configuration example](https://github.com/hfrances/qckdev.AspNetCore.ForwardedHeaders#configure-nginx-as-a-reverse-proxy) covers forwarding headers and path prefixes.

Existing `UseIpSafeFilter(...)` integrations remain available for compatibility. For new applications, use the separate `UseForwardedHeaders(...)` setup above. Do not register both: `UseIpSafeFilter(...)` also adds forwarded-headers middleware.

### Show protection in Swagger

Install the optional package:

~~~bash
dotnet add package qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger
~~~

Configure metadata generation, serve the embedded assets, and enable the UI badges:

~~~csharp
using qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger;

// Service registration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
    options.AddIpSafeSwagger();
});

// Application pipeline, before app.Run()
app.UseIpSafeSwaggerUiStaticFiles();
app.UseSwagger(setupAction: null);
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "v1");
    options.UseIpSafeSwaggerUi();
});
~~~

The badges identify actions selected for IP filtering; they do not confirm that their configured lists contain rules or that a particular client is allowed. See the [Swagger package README](qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger/README.md) for details.

## 🛡️ Cybersecurity

### Check missing rules and scheme names

IpSafe treats an empty list as unrestricted access. Registering a provider or applying an attribute alone does not guarantee that a request will be rejected.

| Configuration on an action with `[IpSafeFilter]` | Result |
| --- | --- |
| Non-empty rules matching the client IP | IP check passes |
| Non-empty rules not matching the client IP | `403 Forbidden` |
| Provider returns `null`, or both client lists have no rules | IP filtering is skipped |
| A scheme-aware provider resolves no client rules for any selected scheme | IP filtering is skipped |

A misspelled scheme or missing configuration section can therefore leave an action unrestricted. If some selected schemes have rules and others are missing or empty, the rules that exist are combined. Verify that the expected lists are populated and that a disallowed client is rejected.

Review `[AllowAnyIpAddress]` exceptions too: they explicitly skip IP filtering according to the attribute precedence rules above.

### Trust only your reverse proxies

Only trust proxy addresses you control. A broad `KnownNetworks` range shared with other workloads allows those workloads to supply forwarded client information if they can reach the API. Do not trust arbitrary clients or use a catch-all network such as `0.0.0.0/0`.

Keep `ForwardLimit` consistent with the actual proxy chain and restrict direct access to the backend. Without correct proxy handling, the filter may evaluate the proxy's IP instead of the client's, or accept a forged client IP.

If you also enable `XForwardedHost`, follow the [ForwardedHeaders security guidance](https://github.com/hfrances/qckdev.AspNetCore.ForwardedHeaders) to configure allowed public hosts.

### Verify access before publishing

Test the deployed network path, including its proxies:

1. An allowed client reaches a protected action.
2. A disallowed client receives `403` from the IP filter.
3. Public exceptions remain reachable.
4. Each named scheme rejects clients outside its own rules.
5. Missing settings and misspelled scheme names are detected before deployment.
6. Forwarded headers from untrusted sources cannot impersonate an allowed client.

IP filtering identifies a network origin, not a user. Clients may share an address through NAT or a proxy; retain authentication and authorization where the endpoint requires them.

The [examples guide](examples/README.md) includes runnable applications and a Docker proxy demonstration.

## 🤝 Contributing

Issues and pull requests are welcome! See the contribution guidelines (coming soon). For the repository's test setup, see the [Integration Testing Guide](docs/TESTING.md).

## 📜 License

This project is licensed under the terms of the [MIT License](LICENSE).
