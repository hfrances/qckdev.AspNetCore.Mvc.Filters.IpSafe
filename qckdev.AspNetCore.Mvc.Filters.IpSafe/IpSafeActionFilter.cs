using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
#if NET10a_0_OR_GREATER
using IPNetwork = System.Net.IPNetwork;
#else
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
#endif

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
            var settings = await SettingsProvider.GetSettingsAsync(context.HttpContext.RequestAborted);
            var properties = IpSafeHelper.GetIpSafeProperties(settings);
            var remoteIp = IpSafeHelper.GetRemoteIpToIpv4(context.HttpContext);
            var allowAny = context.Filters.OfType<AllowAnyIpAddressAttribute>().Any();
            var endpoint = context.HttpContext.Request.Path;
            
            Logger.LogDebug($"IP {(remoteIp?.ToString() ?? "<unknown>")} made a request to endpoint: {(endpoint.ToString() ?? "<unknown>")}");
            if (allowAny)
            {
                // Do nothing. AllowAnyIp attribute set.
            }
            else if (properties.IpAddresses.Any() || properties.IpNetworks.Any())
            {
                if (remoteIp == null)
                {
                    throw new ArgumentException("Remote IP is NULL, may due to missing ForwardedHeaders.");
                }
                else if (!properties.IpAddresses.Contains(remoteIp) && !properties.IpNetworks.Any(x => x.Contains(remoteIp)))
                {
                    Logger.LogWarning($"Request rejected for IP {(remoteIp?.ToString() ?? "<unknown>")} to endpoint: {(endpoint.ToString() ?? "<unknown>")}");
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                // Do Nothing. No restrictions defined.
            }
        }

    }
}
