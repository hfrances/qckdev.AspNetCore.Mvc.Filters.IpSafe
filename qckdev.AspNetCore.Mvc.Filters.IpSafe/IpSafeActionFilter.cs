using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <remarks>
    /// https://code-maze.com/filters-in-asp-net-core-mvc/
    /// https://code-maze.com/action-filters-aspnetcore/
    /// </remarks>
    sealed class IpSafeActionFilter : IActionFilter
    {
        IOptions<IpSafeListSettings> IpSafeListSettings { get; }
        ILogger Logger { get; }

        public IpSafeActionFilter(IOptions<IpSafeListSettings> ipSafeListSettings, ILogger<IpSafeActionFilter> logger)
        {
            this.IpSafeListSettings = ipSafeListSettings;
            this.Logger = logger;
        }

        public void OnAuthorization(AuthorizationFilterContext _)
        {
            // Do Nothing;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            Validate(context);
        }

        public void OnActionExecuted(ActionExecutedContext _)
        {
            // Do Nothing.
        }

        private void Validate(ActionExecutingContext context)
        {
            var properties = IpSafeHelper.GetIpSafeProperties(IpSafeListSettings.Value);
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
