using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    static class IpSafeHelper
    {

        /// <summary>
        /// Gets the IP address of the remote target in Ipv4. Can be null.
        /// </summary>
        /// <returns></returns>
        public static IPAddress? GetRemoteIpToIpv4(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress;

            if (remoteIp != null && remoteIp.IsIPv4MappedToIPv6)
            {
                remoteIp = remoteIp.MapToIPv4();
            }
            return remoteIp;
        }

        public static IpSafeProperties GetIpSafeProperties(IpSafeListSettings? settings)
        {
            var ipAddresses = settings?.IpAddresses?.Select(IPAddress.Parse) ?? Array.Empty<IPAddress>();
            var ipNetworks = settings?.IpNetworks?.Select(IPNetwork2.Parse) ?? Array.Empty<IPNetwork2>();
            var knownProxies = settings?.KnownProxies?.Select(IPAddress.Parse) ?? Array.Empty<IPAddress>();

            return new IpSafeProperties(ipAddresses, ipNetworks, knownProxies);
        }

    }
}
