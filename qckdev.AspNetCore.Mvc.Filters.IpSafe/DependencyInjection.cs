using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods to enhance security by validating incoming requests based on IP addresses.
    /// </summary>
    public static class QIpSafeDependencyInjection
    {

        /// <summary>
        /// Add IP address validation for incoming requests.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IServiceCollection AddIpSafeFilter(this IServiceCollection services, IpSafeListSettings settings)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.Configure<IpSafeListSettings>(config =>
            {
                config.IpAddresses = settings?.IpAddresses;
                config.IpNetworks = settings?.IpNetworks;
                config.KnownProxies = settings?.KnownProxies;
            });
            return services;
        }

        /// <summary>
        /// Add IP address validation for incoming requests.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <remarks>https://stackoverflow.com/questions/36352215/asp-net-core-how-to-get-remote-ip-address</remarks>
        public static IApplicationBuilder UseIpSafeFilter(this IApplicationBuilder builder, Action<ForwardedHeadersOptions>? options = null)
        {
            ILogger logger = builder.ApplicationServices.GetRequiredService<ILogger<IpSafeListSettings>>();
            var opt = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            SetKnownNetworksAndProxies(opt, builder, logger);
            options?.Invoke(opt);
            builder.UseForwardedHeaders(opt);
            logger.LogInformation($"KnownNetworks: ({opt.KnownNetworks.Count}) {string.Join("; ", opt.KnownNetworks.Select(x => $"{x.Prefix}/{x.PrefixLength}"))}".TrimEnd());
            logger.LogInformation($"KnownProxies: ({opt.KnownProxies.Count}) {string.Join("; ", opt.KnownProxies.Select(x => x.ToString()))}".TrimEnd());
            return builder;
        }

        static void SetKnownNetworksAndProxies(ForwardedHeadersOptions opt, IApplicationBuilder builder, ILogger logger)
        {
            
            try
            {
                IpSafeListSettings settings = builder.ApplicationServices.GetRequiredService<IOptions<IpSafeListSettings>>().Value;
                IpSafeProperties properties = IpSafeHelper.GetIpSafeProperties(settings);
                var ipNetworks = new HashSet<IPNetwork>(opt.KnownNetworks, new IpNetworkComparer());

                foreach (var network in properties.IpNetworks ?? Array.Empty<IPNetwork2>())
                {
                    ipNetworks.Add(new IPNetwork(network.Network, network.Cidr));
                }
                foreach (var address in properties.IpAddresses ?? Array.Empty<IPAddress>())
                {
                    ipNetworks.Add(IpSafeHelper.GetNetworkForIP(address));
                }
                opt.KnownNetworks.Clear();
                foreach (var network in ipNetworks)
                {
                    opt.KnownNetworks.Add(network);
                }

                if (settings.KnownProxies == null)
                {
                    opt.KnownProxies.Clear();
                }
                else
                {
                    foreach (var address in properties.KnownProxies)
                    {
                        opt.KnownProxies.Add(address);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

    }
}
