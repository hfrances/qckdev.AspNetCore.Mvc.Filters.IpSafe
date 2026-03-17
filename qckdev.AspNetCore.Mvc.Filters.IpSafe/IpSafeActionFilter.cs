using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <remarks>
    /// https://code-maze.com/filters-in-asp-net-core-mvc/
    /// https://code-maze.com/action-filters-aspnetcore/
    /// </remarks>
    sealed class IpSafeActionFilter : IActionFilter
    {
        IIpSafeSettingsProvider SettingsProvider { get; }
        ILogger Logger { get; }

        public IpSafeActionFilter(IIpSafeSettingsProvider settingsProvider, ILogger<IpSafeActionFilter> logger)
        {
            this.SettingsProvider = settingsProvider;
            this.Logger = logger;
        }

        public void OnAuthorization(AuthorizationFilterContext _)
        {
            // Do Nothing;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateAsync(context).GetAwaiter().GetResult();
        }

        public void OnActionExecuted(ActionExecutedContext _)
        {
            // Do Nothing.
        }

        private async Task ValidateAsync(ActionExecutingContext context)
        {
            var resolution = ResolveIpSafe(context);
            var remoteIp = IpSafeHelper.GetRemoteIpToIpv4(context.HttpContext);
            var endpoint = context.HttpContext.Request.Path;

            Logger.LogDebug($"IP {(remoteIp?.ToString() ?? "<unknown>")} made a request to endpoint: {(endpoint.ToString() ?? "<unknown>")}");
            if (!resolution.EnforceIpSafe)
            {
                // Do nothing. Endpoint/controller resolved as AllowAny or not IpSafe.
                return;
            }

            var settings = await GetSettingsAsync(resolution.Schemes, context.HttpContext.RequestAborted);
            var properties = IpSafeHelper.GetIpSafeProperties(settings);
            if (!(properties.IpAddresses.Any() || properties.IpNetworks.Any()))
            {
                // Do Nothing. No restrictions defined.
                return;
            }

            if (remoteIp == null)
            {
                throw new ArgumentException("Remote IP is NULL, may due to missing ForwardedHeaders.");
            }

            if (!properties.IpAddresses.Contains(remoteIp) && !properties.IpNetworks.Any(x => x.Contains(remoteIp)))
            {
                Logger.LogWarning($"Request rejected for IP {(remoteIp?.ToString() ?? "<unknown>")} to endpoint: {(endpoint.ToString() ?? "<unknown>")}");
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
        }

        async Task<IpSafeListSettings?> GetSettingsAsync(string[] schemes, CancellationToken cancellationToken)
        {
            if (schemes == null || schemes.Length == 0)
            {
                return await SettingsProvider.GetSettingsAsync(cancellationToken);
            }

            if (!(SettingsProvider is IIpSafeSchemeSettingsProvider schemeSettingsProvider))
            {
                // Backward compatibility: providers without scheme support still work.
                return await SettingsProvider.GetSettingsAsync(cancellationToken);
            }

            var settings = new List<IpSafeListSettings>();
            foreach (var scheme in schemes)
            {
                var schemeSettings = await schemeSettingsProvider.GetSettingsAsync(scheme, cancellationToken);
                if (schemeSettings != null)
                {
                    settings.Add(schemeSettings);
                }
            }

            return Merge(settings);
        }

        static IpSafeListSettings? Merge(IEnumerable<IpSafeListSettings> settings)
        {
            if (settings == null)
            {
                return null;
            }

            var ipAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ipNetworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var knownProxies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in settings)
            {
                AddTokens(ipAddresses, item.IpAddresses);
                AddTokens(ipNetworks, item.IpNetworks);
                AddTokens(knownProxies, item.KnownProxies);
            }

            if (ipAddresses.Count == 0 && ipNetworks.Count == 0 && knownProxies.Count == 0)
            {
                return null;
            }

            return new IpSafeListSettings
            {
                IpAddresses = (ipAddresses.Count == 0 ? null : string.Join(";", ipAddresses)),
                IpNetworks = (ipNetworks.Count == 0 ? null : string.Join(";", ipNetworks)),
                KnownProxies = (knownProxies.Count == 0 ? string.Empty : string.Join(";", knownProxies)),
            };
        }

        static void AddTokens(HashSet<string> set, string? value)
        {
            foreach (var token in SplitTokens(value))
            {
                set.Add(token);
            }
        }

        static IEnumerable<string> SplitTokens(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);
        }

        static IpSafeResolution ResolveIpSafe(ActionExecutingContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                var endpointDecision = GetLocalDecision(descriptor.MethodInfo);
                if (endpointDecision != null)
                {
                    return endpointDecision;
                }

                var controllerDecision = GetLocalDecision(descriptor.ControllerTypeInfo);
                if (controllerDecision != null)
                {
                    return controllerDecision;
                }
            }

            // Fallback for non-controller descriptors.
            if (context.Filters.OfType<AllowAnyIpAddressAttribute>().Any())
            {
                return IpSafeResolution.NoEnforce;
            }

            var fallbackSchemes = ExtractSchemes(context.Filters.OfType<IpSafeFilterAttribute>());
            if (context.Filters.OfType<IpSafeFilterAttribute>().Any())
            {
                return IpSafeResolution.Enforce(fallbackSchemes);
            }

            return IpSafeResolution.NoEnforce;
        }

        static IpSafeResolution? GetLocalDecision(MemberInfo memberInfo)
        {
            if (memberInfo.GetCustomAttributes(typeof(AllowAnyIpAddressAttribute), true).Any())
            {
                return IpSafeResolution.NoEnforce;
            }

            var filters = memberInfo.GetCustomAttributes(typeof(IpSafeFilterAttribute), true).OfType<IpSafeFilterAttribute>().ToArray();
            if (filters.Any())
            {
                return IpSafeResolution.Enforce(ExtractSchemes(filters));
            }

            return null;
        }

        static string[] ExtractSchemes(IEnumerable<IpSafeFilterAttribute> filters)
        {
            return (filters ?? Array.Empty<IpSafeFilterAttribute>())
                .SelectMany(x => x.Schemes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        sealed class IpSafeResolution
        {
            public static readonly IpSafeResolution NoEnforce = new IpSafeResolution(false, Array.Empty<string>());

            public bool EnforceIpSafe { get; }
            public string[] Schemes { get; }

            IpSafeResolution(bool enforceIpSafe, string[] schemes)
            {
                this.EnforceIpSafe = enforceIpSafe;
                this.Schemes = (schemes ?? Array.Empty<string>());
            }

            public static IpSafeResolution Enforce(string[] schemes)
            {
                return new IpSafeResolution(true, schemes);
            }
        }
    }
}
