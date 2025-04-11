using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

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
            var ipAddresses =
                settings?.IpAddresses?
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(IPAddress.Parse)
                ?? Array.Empty<IPAddress>();
            var ipNetworks =
                settings?.IpNetworks?
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(IPNetwork2.Parse)
                ?? Array.Empty<IPNetwork2>();
            var knownProxies =
                settings?.KnownProxies?
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(IPAddress.Parse)
                ?? Array.Empty<IPAddress>();

            return new IpSafeProperties(ipAddresses, ipNetworks, knownProxies);
        }

        /// <summary>
        /// Returns the <see cref="IPNetwork"/> where the <see cref="IPAddress"/> is in.
        /// </summary>
        /// <param name="ipAddress">Thw <see cref="IPAddress"/> to calculate the network.</param>
        /// <returns>The <see cref="IPNetwork"/> where the <see cref="IPAddress"/> is in.</returns>
        /// <exception cref="InvalidOperationException">Invalid <paramref name="ipAddress"/> format.</exception>
        public static IPNetwork GetNetworkForIP(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
                return GetNetworkForIPV4(ipAddress, 24); // default mask /24
            }
            else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
            {
                return GetNetworkForIPV6(ipAddress, 64); // default mask /64
            }
            throw new InvalidOperationException("Not supported IP address.");
        }

        // Method to get the network for IPv4
        private static IPNetwork GetNetworkForIPV4(IPAddress ipAddress, int subnetMask)
        {
            var ipBytes = ipAddress.GetAddressBytes();
            var maskBytes = GetSubnetMask(subnetMask);

            // Perform bitwise AND between the IP and the subnet mask
            var networkBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }

            // Adjust the network address to the next valid address (.1 instead of .0)
            networkBytes[3] = (byte)(networkBytes[3] + 1);

            var network = new IPAddress(networkBytes);
            return new IPNetwork(network, subnetMask);
        }

        // Method to get the network for IPv6
        private static IPNetwork GetNetworkForIPV6(IPAddress ipAddress, int subnetMask)
        {
            var ipBytes = ipAddress.GetAddressBytes();
            var maskBytes = GetSubnetMaskV6(subnetMask);

            // Perform bitwise AND between the IP and the subnet mask
            var networkBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }

            // Adjust the network address to the next valid address (.1 instead of .0)
            networkBytes[15] = (byte)(networkBytes[15] + 1);

            var network = new IPAddress(networkBytes);
            return new IPNetwork(network, subnetMask);
        }

        // Get the subnet mask in bytes for IPv4
        private static byte[] GetSubnetMask(int subnetMask)
        {
            var mask = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                if (subnetMask > 8)
                {
                    mask[i] = 255;
                    subnetMask -= 8;
                }
                else
                {
                    mask[i] = (byte)(255 << (8 - subnetMask));
                    subnetMask = 0;
                }
            }
            return mask;
        }

        // Get the subnet mask in bytes for IPv6
        private static byte[] GetSubnetMaskV6(int subnetMask)
        {
            var mask = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                if (subnetMask > 8)
                {
                    mask[i] = 255;
                    subnetMask -= 8;
                }
                else
                {
                    mask[i] = (byte)(255 << (8 - subnetMask));
                    subnetMask = 0;
                }
            }
            return mask;
        }

    }
}
