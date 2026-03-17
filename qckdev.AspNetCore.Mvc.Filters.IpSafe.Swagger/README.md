[![NuGet Version](https://img.shields.io/nuget/v/qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger.svg)](https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger&metric=alert_status)](https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger)
[![Code Coverage](https://sonarcloud.io/api/project_badges/measure?project=qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger&metric=coverage)](https://sonarcloud.io/dashboard?id=qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger)
![Azure Pipelines Status](https://hfrances.visualstudio.com/qckdev/_apis/build/status/qckdev.AspNetCore.Mvc.Filters.IpSafe?branchName=master)

# qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger

Swagger/OpenAPI integration for `qckdev.AspNetCore.Mvc.Filters.IpSafe`.
It marks endpoints protected by `IpSafeFilter` and adds a visual IP badge in Swagger UI.

## 🛠️ Installation

```bash
dotnet add package qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger
```

## ⚡ Quick Start

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger;

// Configure Swagger generation
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
    c.AddIpSafeSwagger();
});

// Configure HTTP pipeline
if (env.IsDevelopment())
{
    app.UseIpSafeSwaggerUiStaticFiles();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        c.UseIpSafeSwaggerUi();
    });
}
```

### How it works

- Adds `x-ip-safe: true` to operations protected by `IpSafeFilter`.
- Skips endpoints with `AllowAnyIpAddress`.
- Injects embedded JS/CSS to render an `IP` badge in Swagger UI operation rows.

## 🤝 Contributing
Issues and pull requests are welcome! See the contribution guidelines (coming soon).

## 📜 License
This project is licensed under the terms of the [MIT License](LICENSE).
