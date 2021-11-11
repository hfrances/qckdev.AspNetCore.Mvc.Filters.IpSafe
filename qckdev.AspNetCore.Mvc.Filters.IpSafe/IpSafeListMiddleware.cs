using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Middlewares
{
    public sealed class IpSafeListMiddleware
    {

        RequestDelegate Next { get; }
        IOptions<IpSafeListSettings> IpSafeListSettings { get; }

        public IpSafeListMiddleware(RequestDelegate next, IOptions<IpSafeListSettings> ipSafeListSettings)
        {
            this.Next = next;
            this.IpSafeListSettings = ipSafeListSettings;
        }

        public async Task Invoke(HttpContext context)
        {
            var ipAddresses = IpSafeListSettings.Value.IpAddresses?.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(IPAddress.Parse) ?? Array.Empty<IPAddress>();
            var ipNetworks = IpSafeListSettings.Value.IpNetworks?.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(IPNetwork.Parse) ?? Array.Empty<IPNetwork>();
            var remoteIp = context.Connection.RemoteIpAddress;

            if (remoteIp == null)
            {
                throw new ArgumentException("Remote IP is NULL, may due to missing ForwardedHeaders.");
            }
            else
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    remoteIp = remoteIp.MapToIPv4();
                }

                if (!ipAddresses.Contains(remoteIp) && !ipNetworks.Any(x => x.Contains(remoteIp)))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                }
            }
            await Next(context);
        }

    }
}
