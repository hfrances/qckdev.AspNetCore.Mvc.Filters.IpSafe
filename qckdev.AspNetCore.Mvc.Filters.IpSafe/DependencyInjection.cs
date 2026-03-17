using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if NET10a_0_OR_GREATER
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
        const ForwardedHeaders DefaultForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        const int DefaultForwardLimit = 1;

        /// <summary>
        /// Adds a named IpSafe settings scheme.
        /// This is additive and does not replace existing default IpSafe settings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="scheme">The scheme name.</param>
        /// <param name="configureSettings">A delegate that allows configuring IpSafeListSettings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeScheme(
            this IServiceCollection services,
            string scheme,
            Action<IpSafeListSettings> configureSettings)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                throw new ArgumentException("Scheme name cannot be null or empty.", nameof(scheme));
            }
            if (configureSettings == null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            services.Configure(scheme, configureSettings);
            return services;
        }

        /// <summary>
        /// Adds a named IpSafe settings scheme from a static settings instance.
        /// This is additive and does not replace existing default IpSafe settings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="scheme">The scheme name.</param>
        /// <param name="settings">The static settings for the scheme.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeScheme(
            this IServiceCollection services,
            string scheme,
            IpSafeListSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return AddIpSafeScheme(services, scheme, config =>
            {
                config.IpAddresses = settings.IpAddresses;
                config.IpNetworks = settings.IpNetworks;
                config.KnownProxies = settings.KnownProxies;
            });
        }

        /// <summary>
        /// Add IP address validation using a custom settings provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the IP Safe settings provider.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeFilter<TProvider>(this IServiceCollection services)
            where TProvider : class, IIpSafeSettingsProvider
        {
            return AddIpSafeFilterCore<TProvider>(services, null);
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
            return AddIpSafeFilterCore<TProvider>(services, configureSettings);
        }

        /// <summary>
        /// Add IP address validation using default settings provider from IpSafeListSettings instance.
        /// [OBSOLETE] Use AddIpSafeFilter&lt;IpSafeSettingsProvider&gt;(Action&lt;IpSafeListSettings&gt;) for inline configuration instead.
        /// </summary>
        [Obsolete("Use AddIpSafeFilter<IpSafeSettingsProvider>(opts => { ... }) for inline configuration instead.", false)]
        public static IServiceCollection AddIpSafeFilter(this IServiceCollection services, IpSafeListSettings settings)
        {
            return AddIpSafeFilterCore<IpSafeSettingsProvider>(services, config =>
            {
                config.IpAddresses = settings?.IpAddresses;
                config.IpNetworks = settings?.IpNetworks;
                config.KnownProxies = settings?.KnownProxies;
            });
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
            return ConfigureUseIpSafeFilter(builder, options, null, null);
        }

        /// <summary>
        /// Add IP address validation and configure trusted proxies/networks using a fluent builder.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="configure">Configuration callback.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseIpSafeFilter(
            this IApplicationBuilder builder,
            Action<IpSafeForwardedHeadersBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var cfg = new IpSafeForwardedHeadersBuilder();
            configure(cfg);
            return ConfigureUseIpSafeFilter(builder, null, null, cfg);
        }

        /// <summary>
        /// Adds IP address validation and allows configuring forwarded headers options explicitly.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="forwardedHeadersOptions">Forwarded headers options instance (same style as UseForwardedHeaders overload).</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseIpSafeFilter(
            this IApplicationBuilder builder,
            ForwardedHeadersOptions forwardedHeadersOptions)
        {
            if (forwardedHeadersOptions == null)
            {
                throw new ArgumentNullException(nameof(forwardedHeadersOptions));
            }

            return ConfigureUseIpSafeFilter(builder, opt =>
            {
                CopyForwardedHeadersOptions(forwardedHeadersOptions, opt);
            }, null, null);
        }

        /// <summary>
        /// Add IP address validation using static settings (legacy overload).
        /// [OBSOLETE] Use UseIpSafeFilter() without options parameter after AddIpSafeFilter&lt;T&gt;() instead.
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
            ConfigureForwardedHeadersFromSettings(opt, settings);
            options?.Invoke(opt);
            LogForwardedHeadersSafetyWarnings(logger, opt);
            builder.UseForwardedHeaders(opt);
            LogKnownForwardedHeaders(logger, opt);

            return builder;
        }

        static IApplicationBuilder ConfigureUseIpSafeFilter(
            IApplicationBuilder builder,
            Action<ForwardedHeadersOptions>? options,
            Func<IServiceScope, IpSafeTrustedProxiesSettings?>? trustedProxiesResolver,
            IpSafeForwardedHeadersBuilder? forwardedHeadersBuilder)
        {
            ILogger logger = builder.ApplicationServices.GetRequiredService<ILogger<IpSafeListSettings>>();
            using var scope = builder.ApplicationServices.CreateScope();
            var settingsProvider = scope.ServiceProvider.GetRequiredService<IIpSafeSettingsProvider>();

            var opt = CreateDefaultForwardedHeadersOptions();

            // Retrieve settings from provider at startup.
            var settings = settingsProvider.GetSettingsAsync().GetAwaiter().GetResult();
            SetKnownNetworksAndProxies(opt, settings, logger);
            ConfigureForwardedHeadersFromSettings(opt, settings);

            ApplyTrustedProxiesFromResolver(opt, logger, scope, trustedProxiesResolver);
            ApplyTrustedProxiesFromBuilder(opt, logger, scope, forwardedHeadersBuilder);

            options?.Invoke(opt);
            LogForwardedHeadersSafetyWarnings(logger, opt);
            builder.UseForwardedHeaders(opt);
            LogKnownForwardedHeaders(logger, opt);

            return builder;
        }

        static void SetKnownNetworksAndProxies(
            ForwardedHeadersOptions opt,
            IpSafeListSettings? settings,
            ILogger logger)
        {
            var properties = TryGetIpSafeProperties(settings, logger);
            if (properties == null)
            {
                return;
            }

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

            opt.KnownProxies.Clear();
            if (settings?.KnownProxies == null)
            {
                return;
            }

            foreach (var address in properties.KnownProxies)
            {
                opt.KnownProxies.Add(address);
            }
        }

        static IpSafeTrustedProxiesSettings? ResolveTrustedProxiesFromBuilder(
            IServiceScope scope,
            IpSafeForwardedHeadersBuilder builder)
        {
            if (builder.TrustedProxiesProviderType == null)
            {
                return null;
            }

            var provider = scope.ServiceProvider.GetRequiredService(builder.TrustedProxiesProviderType) as IIpSafeTrustedProxiesProvider;
            if (provider == null)
            {
                return null;
            }

            return provider.GetSettingsAsync().GetAwaiter().GetResult();
        }

        static void SetTrustedProxies(
            ForwardedHeadersOptions opt,
            IpSafeTrustedProxiesSettings? settings,
            ILogger logger)
        {
            if (settings == null)
            {
                return;
            }

            var properties = TryGetIpSafeProperties(new IpSafeListSettings
            {
                IpAddresses = null,
                IpNetworks = settings.KnownNetworks,
                KnownProxies = settings.KnownProxies
            }, logger);

            if (properties == null)
            {
                return;
            }

            ApplyTrustedNetworksAndProxies(
                opt,
                properties.IpNetworks ?? Array.Empty<IPNetwork2>(),
                properties.KnownProxies ?? Array.Empty<IPAddress>());
        }

        static void ConfigureForwardedHeadersFromSettings(
            ForwardedHeadersOptions opt,
            IpSafeListSettings? settings)
        {
            if (settings is IIpSafeForwardedHeadersSource source)
            {
                source.ConfigureForwardedHeaders(opt);
            }
        }

        static void CopyForwardedHeadersOptions(ForwardedHeadersOptions source, ForwardedHeadersOptions target)
        {
            target.ForwardedHeaders = source.ForwardedHeaders;
            target.ForwardLimit = source.ForwardLimit;
            target.KnownProxies.Clear();
            foreach (var proxy in source.KnownProxies)
            {
                target.KnownProxies.Add(proxy);
            }

            ClearKnownNetworks(target);
            foreach (var network in GetKnownNetworks(source))
            {
                AddKnownNetwork(target, network);
            }
        }

        static IServiceCollection AddIpSafeFilterCore<TProvider>(
            IServiceCollection services,
            Action<IpSafeListSettings>? configureSettings)
            where TProvider : class, IIpSafeSettingsProvider
        {
            ConfigureDefaultForwardedHeaders(services);

            if (configureSettings != null)
            {
                services.Configure<IpSafeListSettings>(configureSettings);
            }

            services.AddScoped<IIpSafeSettingsProvider, TProvider>();
            return services;
        }

        static void ConfigureDefaultForwardedHeaders(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = DefaultForwardedHeaders;
                options.ForwardLimit = DefaultForwardLimit;
            });
        }

        static ForwardedHeadersOptions CreateDefaultForwardedHeadersOptions()
        {
            return new ForwardedHeadersOptions
            {
                ForwardedHeaders = DefaultForwardedHeaders,
                ForwardLimit = DefaultForwardLimit
            };
        }

        static void ApplyTrustedProxiesFromResolver(
            ForwardedHeadersOptions opt,
            ILogger logger,
            IServiceScope scope,
            Func<IServiceScope, IpSafeTrustedProxiesSettings?>? trustedProxiesResolver)
        {
            if (trustedProxiesResolver == null)
            {
                return;
            }

            SetTrustedProxies(opt, trustedProxiesResolver(scope), logger);
        }

        static void ApplyTrustedProxiesFromBuilder(
            ForwardedHeadersOptions opt,
            ILogger logger,
            IServiceScope scope,
            IpSafeForwardedHeadersBuilder? builder)
        {
            if (builder == null)
            {
                return;
            }

            var trustedSettingsFromBuilder = ResolveTrustedProxiesFromBuilder(scope, builder);
            SetTrustedProxies(opt, trustedSettingsFromBuilder, logger);

            if ((builder.KnownProxies != null && builder.KnownProxies.Count > 0) ||
                (builder.KnownNetworks != null && builder.KnownNetworks.Count > 0))
            {
                var manualSettings = new IpSafeTrustedProxiesSettings
                {
                    KnownProxies = builder.KnownProxies == null || builder.KnownProxies.Count == 0
                        ? string.Empty
                        : string.Join(";", builder.KnownProxies),
                    KnownNetworks = builder.KnownNetworks == null || builder.KnownNetworks.Count == 0
                        ? string.Empty
                        : string.Join(";", builder.KnownNetworks)
                };

                SetTrustedProxies(opt, manualSettings, logger);
            }

            foreach (var configureForwardedHeaders in builder.ForwardedHeadersConfigurations)
            {
                configureForwardedHeaders(opt);
            }
        }

        static void ApplyTrustedNetworksAndProxies(
            ForwardedHeadersOptions opt,
            IEnumerable<IPNetwork2> knownNetworks,
            IEnumerable<IPAddress> knownProxies)
        {
            ClearKnownNetworks(opt);
            foreach (var network in knownNetworks)
            {
                AddKnownNetwork(opt, new PlatformIPNetwork(network.Network, network.Cidr));
            }

            opt.KnownProxies.Clear();
            foreach (var proxy in knownProxies)
            {
                opt.KnownProxies.Add(proxy);
            }
        }

        static IpSafeProperties? TryGetIpSafeProperties(IpSafeListSettings? settings, ILogger logger)
        {
            try
            {
                return IpSafeHelper.GetIpSafeProperties(settings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return null;
            }
        }

        static void LogForwardedHeadersSafetyWarnings(ILogger logger, ForwardedHeadersOptions options)
        {
            var knownNetworksCount = GetKnownNetworks(options).Count();
            var knownProxiesCount = options.KnownProxies.Count;

            if (knownNetworksCount == 0)
            {
                logger.LogCritical("CRITICAL SECURITY WARNING: KnownNetworks is empty. " +
                    "If KnownProxies/KnownNetworks are not correctly configured for your reverse proxies, " +
                    "Forwarded headers may be spoofed and IP-based restrictions can be bypassed.");
            }

            if (knownNetworksCount == 0 && knownProxiesCount == 0)
            {
                logger.LogCritical("CRITICAL SECURITY WARNING: Both KnownNetworks and KnownProxies are empty. " +
                    "Do not trust X-Forwarded-* headers in this state in production.");
            }
        }

        static void LogKnownForwardedHeaders(ILogger logger, ForwardedHeadersOptions options)
        {
            var knownNetworks = GetKnownNetworks(options).ToArray();
            logger.LogInformation($"KnownNetworks: ({knownNetworks.Length}) {string.Join("; ", knownNetworks.Select(x => $"{GetNetworkAddress(x)}/{x.PrefixLength}"))}".TrimEnd());
            logger.LogInformation($"KnownProxies: ({options.KnownProxies.Count}) {string.Join("; ", options.KnownProxies.Select(x => x.ToString()))}".TrimEnd());
        }

#if NET10a_0_OR_GREATER
        private static IPAddress GetNetworkAddress(PlatformIPNetwork network) => network.BaseAddress;
        private static IEnumerable<PlatformIPNetwork> GetKnownNetworks(ForwardedHeadersOptions options) => options.KnownNetworks;
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
