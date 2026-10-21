using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using qckdev.AspNetCore.ForwardedHeaders;
using ForwardedHeadersType = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders;
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
        const ForwardedHeadersType DefaultForwardedHeaders = ForwardedHeadersType.XForwardedFor | ForwardedHeadersType.XForwardedProto;
        const int DefaultForwardLimit = 1;

        /// <summary>
        /// Adds a named IpSafe settings scheme.
        /// This is additive and does not replace existing default IpSafe settings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="scheme">The scheme name.</param>
        /// <param name="configureOptions">Configures the named options builder.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeFilter(
            this IServiceCollection services,
            string scheme,
            Action<OptionsBuilder<IpSafeListSettings>> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (string.IsNullOrWhiteSpace(scheme))
            {
                throw new ArgumentException("Scheme name cannot be null or empty.", nameof(scheme));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            configureOptions(services.AddOptions<IpSafeListSettings>(scheme));
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
        public static IServiceCollection AddIpSafeFilter(
            this IServiceCollection services,
            string scheme,
            IpSafeListSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return AddIpSafeFilter(services, scheme, options => 
                options.Configure(config =>
                {
                    config.IpAddresses = settings.IpAddresses;
                    config.IpNetworks = settings.IpNetworks;
                    config.KnownProxies = settings.KnownProxies;
                }));
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
        /// Add IP address validation with settings bound from a configuration section.
        /// </summary>
        /// <typeparam name="TProvider">The type of the IP Safe settings provider.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="sectionName">The configuration section name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeFilter<TProvider>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName)
            where TProvider : class, IIpSafeSettingsProvider
        {
            return AddIpSafeFilter<TProvider>(services, options =>
                options.WithConfiguration(configuration, sectionName));
        }

        /// <summary>
        /// Add IP address validation with a custom settings provider and options builder configuration.
        /// </summary>
        /// <typeparam name="TProvider">The type of the IP Safe settings provider.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configures the default options builder.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIpSafeFilter<TProvider>(
            this IServiceCollection services,
            Action<OptionsBuilder<IpSafeListSettings>> configureOptions)
            where TProvider : class, IIpSafeSettingsProvider
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }
            return AddIpSafeFilterCore<TProvider>(services, configureOptions);
        }

        /// <summary>
        /// Add IP address validation using default settings provider from IpSafeListSettings instance.
        /// [OBSOLETE] Use AddIpSafeFilter&lt;IpSafeSettingsProvider&gt;(options =&gt; options.Configure(...)) instead.
        /// </summary>
        [Obsolete("Use AddIpSafeFilter<IpSafeSettingsProvider>(options => options.Configure(settings => { ... })) instead.", false)]
        public static IServiceCollection AddIpSafeFilter(this IServiceCollection services, IpSafeListSettings settings)
        {
            return AddIpSafeFilterCore<IpSafeSettingsProvider>(services, options => options.Configure(config =>
            {
                config.IpAddresses = settings?.IpAddresses;
                config.IpNetworks = settings?.IpNetworks;
                config.KnownProxies = settings?.KnownProxies;
            }));
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
                ForwardedHeaders = ForwardedHeadersType.XForwardedFor | ForwardedHeadersType.XForwardedProto
            };

            SetKnownProxies(opt, settings, logger);
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
            SetKnownProxies(opt, settings, logger);
            ConfigureForwardedHeadersFromSettings(opt, settings);

            ApplyTrustedProxiesFromResolver(opt, logger, scope, trustedProxiesResolver);
            ApplyTrustedProxiesFromBuilder(opt, logger, scope, forwardedHeadersBuilder);

            options?.Invoke(opt);
            LogForwardedHeadersSafetyWarnings(logger, opt);
            builder.UseForwardedHeaders(opt);
            LogKnownForwardedHeaders(logger, opt);

            return builder;
        }

        static void SetKnownProxies(
            ForwardedHeadersOptions opt,
            IpSafeListSettings? settings,
            ILogger logger)
        {
            var properties = TryGetIpSafeProperties(settings, logger);
            if (properties == null)
            {
                return;
            }

            opt.KnownProxies.Clear();
            if (settings?.KnownProxies == null)
            {
                return;
            }

            opt.AddKnownProxies(properties.KnownProxies.Select(x => x.ToString()));
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
            Action<OptionsBuilder<IpSafeListSettings>>? configureOptions)
            where TProvider : class, IIpSafeSettingsProvider
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            ConfigureDefaultForwardedHeaders(services);
            configureOptions?.Invoke(services.AddOptions<IpSafeListSettings>());

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
            opt.AddKnownNetworks(knownNetworks.Select(x => $"{x.Network}/{x.Cidr}"));

            opt.KnownProxies.Clear();
            opt.AddKnownProxies(knownProxies.Select(x => x.ToString()));
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
