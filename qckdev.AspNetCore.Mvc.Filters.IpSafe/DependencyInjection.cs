using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if NET10_0_OR_GREATER
using PlatformIPNetwork = System.Net.IPNetwork;
#else
using PlatformIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
#endif

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Extension methods to enhance security by validating incoming requests based on IP addresses.
    /// </summary>
    public static class QIpSafeDependencyInjection
    {

        /// <summary>
        /// Add IP address validation using a custom settings provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the IP Safe settings provider.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeFilter<TProvider>(this IServiceCollection services)
            where TProvider : class, IIpSafeSettingsProvider
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddScoped<IIpSafeSettingsProvider, TProvider>();
            return services;
        }

        /// <summary>
        /// Add IP address validation with custom settings provider and IpSafeListSettings configuration.
        /// </summary>
        /// <typeparam name="TProvider">The type of the IP Safe settings provider.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureSettings">A delegate that allows configuring IpSafeListSettings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeFilter<TProvider>(
            this IServiceCollection services,
            Action<IpSafeListSettings>? configureSettings = null)
            where TProvider : class, IIpSafeSettingsProvider
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            if (configureSettings != null)
            {
                services.Configure<IpSafeListSettings>(configureSettings);
            }

            services.AddScoped<IIpSafeSettingsProvider, TProvider>();
            return services;
        }

        /// <summary>
        /// Add IP address validation using default settings provider from IpSafeListSettings instance.
        /// [OBSOLETE] Use AddIpSafeFilter&lt;IpSafeSettingsProvider&gt;(Action&lt;IpSafeListSettings&gt;) for inline configuration instead.
        /// </summary>
        [Obsolete("Use AddIpSafeFilter<IpSafeSettingsProvider>(opts => { ... }) for inline configuration instead.", false)]
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
            
            services.AddScoped<IIpSafeSettingsProvider, IpSafeSettingsProvider>();
            return services;
        }

        /// <summary>
        /// Add IP address validation for incoming requests using the configured settings provider.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="options">Optional delegate to configure ForwardedHeadersOptions.</param>
        /// <remarks>
        /// This method retrieves settings from the registered IIpSafeSettingsProvider.
        /// Settings are resolved at startup time.
        /// https://stackoverflow.com/questions/36352215/asp-net-core-how-to-get-remote-ip-address
        /// </remarks>
        public static IApplicationBuilder UseIpSafeFilter(
            this IApplicationBuilder builder, 
            Action<ForwardedHeadersOptions>? options = null)
        {
            ILogger logger = builder.ApplicationServices.GetRequiredService<ILogger<IpSafeListSettings>>();
            var settingsProvider = builder.ApplicationServices.GetRequiredService<IIpSafeSettingsProvider>();
            
            var opt = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            // Retrieve settings from provider at startup
            var settings = settingsProvider.GetSettingsAsync().GetAwaiter().GetResult();
            SetKnownNetworksAndProxies(opt, settings, logger);
            
            options?.Invoke(opt);
            builder.UseForwardedHeaders(opt);
            
            var knownNetworks = GetKnownNetworks(opt).ToArray();
            logger.LogInformation($"KnownNetworks: ({knownNetworks.Length}) {string.Join("; ", knownNetworks.Select(x => $"{GetNetworkAddress(x)}/{x.PrefixLength}"))}".TrimEnd());
            logger.LogInformation($"KnownProxies: ({opt.KnownProxies.Count}) {string.Join("; ", opt.KnownProxies.Select(x => x.ToString()))}".TrimEnd());
            
            return builder;
        }

        static void SetKnownNetworksAndProxies(
            ForwardedHeadersOptions opt, 
            IpSafeListSettings? settings, 
            ILogger logger)
        {
            try
            {
                IpSafeProperties properties = IpSafeHelper.GetIpSafeProperties(settings);
                var ipNetworks = new HashSet<PlatformIPNetwork>(GetKnownNetworks(opt), new IpNetworkComparer());

                foreach (var network in properties.IpNetworks ?? Array.Empty<IPNetwork2>())
                {
                    ipNetworks.Add(new PlatformIPNetwork(network.Network, network.Cidr));
                }
                foreach (var address in properties.IpAddresses ?? Array.Empty<IPAddress>())
                {
                    var networkFromHelper = IpSafeHelper.GetNetworkForIP(address);
                    ipNetworks.Add(new PlatformIPNetwork(address, networkFromHelper.PrefixLength));
                }

                ClearKnownNetworks(opt);
                foreach (var network in ipNetworks)
                {
                    AddKnownNetwork(opt, network);
                }

                if (settings?.KnownProxies == null)
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

        /// <summary>
        /// Add IP address validation using static settings (legacy overload).
        /// [OBSOLETE] Use UseIpSafeFilter() without parameters after AddIpSafeFilter&lt;T&gt;() instead.
        /// </summary>
        [Obsolete("Use UseIpSafeFilter() without options parameter. " +
                  "Register a custom IIpSafeSettingsProvider using AddIpSafeFilter<T>().", false)]
        public static IApplicationBuilder UseIpSafeFilter(
            this IApplicationBuilder builder, 
            Action<ForwardedHeadersOptions>? options,
            IpSafeListSettings settings)
        {
            ILogger logger = builder.ApplicationServices.GetRequiredService<ILogger<IpSafeListSettings>>();
            
            var opt = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            SetKnownNetworksAndProxies(opt, settings, logger);
            options?.Invoke(opt);
            builder.UseForwardedHeaders(opt);
            
            var knownNetworks = GetKnownNetworks(opt).ToArray();
            logger.LogInformation($"KnownNetworks: ({knownNetworks.Length}) {string.Join("; ", knownNetworks.Select(x => $"{GetNetworkAddress(x)}/{x.PrefixLength}"))}".TrimEnd());
            logger.LogInformation($"KnownProxies: ({opt.KnownProxies.Count}) {string.Join("; ", opt.KnownProxies.Select(x => x.ToString()))}".TrimEnd());
            
            return builder;
        }

#if NET10_0_OR_GREATER
        private static IPAddress GetNetworkAddress(PlatformIPNetwork network) => network.BaseAddress;
        private static IEnumerable<PlatformIPNetwork> GetKnownNetworks(ForwardedHeadersOptions options) => options.KnownIPNetworks;
        private static void ClearKnownNetworks(ForwardedHeadersOptions options) => options.KnownIPNetworks.Clear();
        private static void AddKnownNetwork(ForwardedHeadersOptions options, PlatformIPNetwork network) => options.KnownIPNetworks.Add(network);
#else
        private static IPAddress GetNetworkAddress(PlatformIPNetwork network) => network.Prefix;
        private static IEnumerable<PlatformIPNetwork> GetKnownNetworks(ForwardedHeadersOptions options) => options.KnownNetworks;
        private static void ClearKnownNetworks(ForwardedHeadersOptions options) => options.KnownNetworks.Clear();
        private static void AddKnownNetwork(ForwardedHeadersOptions options, PlatformIPNetwork network) => options.KnownNetworks.Add(network);
#endif

    }
}
