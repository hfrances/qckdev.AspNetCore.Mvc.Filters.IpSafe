using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger
{
    /// <summary>
    /// Extension methods for Swagger UI integration of IP-safe endpoint markers.
    /// </summary>
    public static class QIpSafeSwaggerDependencyInjection
    {
        const string DefaultAssetsRequestPath = "/swagger-ui/ip-safe";

        /// <summary>
        /// Registers IP-safe metadata generation in Swagger operations.
        /// </summary>
        /// <param name="options">Swagger generation options.</param>
        /// <returns>The same options instance.</returns>
        public static SwaggerGenOptions AddIpSafeSwagger(this SwaggerGenOptions options)
        {
            options.OperationFilter<IpSafeOperationFilter>();
            return options;
        }

        /// <summary>
        /// Injects JS/CSS that renders an IP badge for IP-safe endpoints in Swagger UI.
        /// </summary>
        /// <param name="options">Swagger UI options.</param>
        /// <param name="assetsRequestPath">Request path used to serve embedded assets.</param>
        /// <returns>The same options instance.</returns>
        public static SwaggerUIOptions UseIpSafeSwaggerUi(
            this SwaggerUIOptions options,
            string assetsRequestPath = DefaultAssetsRequestPath)
        {
            var path = NormalizeRequestPath(assetsRequestPath);
            options.InjectJavascript($"{path}/ip-safe.js");
            options.InjectStylesheet($"{path}/ip-safe.css");
            return options;
        }

        /// <summary>
        /// Serves embedded Swagger UI assets required to render IP-safe endpoint badges.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="assetsRequestPath">Request path used to serve embedded assets.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseIpSafeSwaggerUiStaticFiles(
            this IApplicationBuilder app,
            string assetsRequestPath = DefaultAssetsRequestPath)
        {
            var path = NormalizeRequestPath(assetsRequestPath);
            var assembly = Assembly.GetExecutingAssembly();
            var jsResourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith(".wwwroot.ip-safe.js", StringComparison.OrdinalIgnoreCase));
            var cssResourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith(".wwwroot.ip-safe.css", StringComparison.OrdinalIgnoreCase));
            var jsPath = path + "/ip-safe.js";
            var cssPath = path + "/ip-safe.css";

            app.Use(async (context, next) =>
            {
                var requestPath = context.Request.Path.Value ?? string.Empty;
                if (requestPath.Equals(jsPath, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteEmbeddedResourceAsync(context, assembly, jsResourceName, "application/javascript").ConfigureAwait(false);
                    return;
                }

                if (requestPath.Equals(cssPath, StringComparison.OrdinalIgnoreCase))
                {
                    await WriteEmbeddedResourceAsync(context, assembly, cssResourceName, "text/css").ConfigureAwait(false);
                    return;
                }

                await next().ConfigureAwait(false);
            });

            return app;
        }

        static async Task WriteEmbeddedResourceAsync(
            Microsoft.AspNetCore.Http.HttpContext context,
            Assembly assembly,
            string? resourceName,
            string contentType)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            context.Response.ContentType = contentType;
            context.Response.StatusCode = 200;

            using (stream)
            {
            await stream.CopyToAsync(context.Response.Body).ConfigureAwait(false);
            }
        }

        static string NormalizeRequestPath(string path)
        {
            var value = string.IsNullOrWhiteSpace(path)
                ? DefaultAssetsRequestPath
                : path.Trim();

            if (!value.StartsWith("/", StringComparison.Ordinal))
            {
                value = "/" + value;
            }

            return value.TrimEnd('/');
        }
    }
}
