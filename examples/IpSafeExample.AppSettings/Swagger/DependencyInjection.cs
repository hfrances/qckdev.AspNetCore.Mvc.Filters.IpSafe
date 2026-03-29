using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Reflection;
using qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger;

namespace IpSafeExample.AppSettings.Swagger
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "IpSafe AppSettings API", Version = "v1" });
                c.AddXmlCommentsFromCurrentAssembly();
                c.AddIpSafeSwagger();
            });

            return services;
        }

        public static SwaggerGenOptions AddXmlCommentsFromCurrentAssembly(this SwaggerGenOptions options)
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            return options;
        }

        public static IApplicationBuilder UseSwagger(this IApplicationBuilder app)
        {
            SwaggerBuilderExtensions.UseSwagger(app);
            app.UseIpSafeSwaggerUiStaticFiles();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "IpSafe AppSettings API v1");
                c.UseIpSafeSwaggerUi();
            });

            return app;
        }
    }
}
