using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Middlewares
{

    sealed class IpSafeListMiddleware
    {

        RequestDelegate Next { get; }
        IIpSafeSettingsProvider SettingsProvider { get; }
        ILogger Logger { get; }

        public IpSafeListMiddleware(RequestDelegate next, IIpSafeSettingsProvider settingsProvider, ILogger<IpSafeListMiddleware> logger)
        {
            this.Next = next;
            this.SettingsProvider = settingsProvider;
            this.Logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var settings = await SettingsProvider.GetSettingsAsync(context.RequestAborted);
            var properties = IpSafeHelper.GetIpSafeProperties(settings);
            var remoteIp = IpSafeHelper.GetRemoteIpToIpv4(context);
            var endpoint = context.Request.Path;

            Logger.LogDebug($"IP {(remoteIp?.ToString() ?? "<unknown>")} made a request to endpoint: {(endpoint.ToString() ?? "<unknown>")}");
            if (properties.IpAddresses.Any() || properties.IpNetworks.Any())
            {
                if (remoteIp == null)
                {
                    throw new ArgumentException("Remote IP is NULL, may due to missing ForwardedHeaders.");
                }
                else if (!properties.IpAddresses.Contains(remoteIp) && !properties.IpNetworks.Any(x => x.Contains(remoteIp)))
                {
                    Logger.LogWarning($"Request rejected for IP {(remoteIp?.ToString() ?? "<unknown>")} to endpoint: {(endpoint.ToString() ?? "<unknown>")}");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                }
            }
            else
            {
                // Do Nothing. No restrictions defined.
            }
            await Next(context);
        }

    }
}
