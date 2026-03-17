using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Builder used by <c>UseIpSafeFilter</c> to configure trusted proxies/networks and forwarded headers options.
    /// </summary>
    public sealed class IpSafeForwardedHeadersBuilder
    {
        readonly List<string> _knownProxies = new List<string>();
        readonly List<string> _knownNetworks = new List<string>();
        readonly List<Action<ForwardedHeadersOptions>> _forwardedHeadersConfigurations = new List<Action<ForwardedHeadersOptions>>();

        internal Type? TrustedProxiesProviderType { get; private set; }
        internal IReadOnlyCollection<string> KnownProxies => _knownProxies;
        internal IReadOnlyCollection<string> KnownNetworks => _knownNetworks;
        internal IReadOnlyCollection<Action<ForwardedHeadersOptions>> ForwardedHeadersConfigurations => _forwardedHeadersConfigurations;

        /// <summary>
        /// Configures trusted proxies/networks through a provider resolved from DI.
        /// 
        /// IMPORTANT: Values are resolved during application startup. If the underlying data changes later,
        /// restart the API to apply the new trusted proxies/networks.
        /// </summary>
        /// <typeparam name="TProvider">Trusted proxies provider type.</typeparam>
        /// <returns>Current builder instance.</returns>
        public IpSafeForwardedHeadersBuilder WithProxyService<TProvider>()
            where TProvider : class, IIpSafeTrustedProxiesProvider
        {
            TrustedProxiesProviderType = typeof(TProvider);
            return this;
        }

        /// <summary>
        /// Adds known proxies manually.
        /// </summary>
        /// <param name="proxies">Proxy IP addresses.</param>
        /// <returns>Current builder instance.</returns>
        public IpSafeForwardedHeadersBuilder WithKnownProxies(params string[] proxies)
        {
            AddNonEmptyDistinct(_knownProxies, proxies);
            return this;
        }

        /// <summary>
        /// Adds known networks manually.
        /// </summary>
        /// <param name="networks">Network CIDR values.</param>
        /// <returns>Current builder instance.</returns>
        public IpSafeForwardedHeadersBuilder WithKnownNetworks(params string[] networks)
        {
            AddNonEmptyDistinct(_knownNetworks, networks);
            return this;
        }

        /// <summary>
        /// Adds known proxies and known networks from a configuration target.
        /// </summary>
        /// <typeparam name="TTarget">Configuration target type.</typeparam>
        /// <param name="configuration">Configuration source.</param>
        /// <param name="bind">Binding callback that populates the target instance.</param>
        /// <returns>Current builder instance.</returns>
        public IpSafeForwardedHeadersBuilder WithConfiguration<TTarget>(
            IConfiguration configuration,
            Action<IConfiguration, TTarget> bind)
            where TTarget : class, IIpSafeForwardedHeadersTarget, new()
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (bind == null)
            {
                throw new ArgumentNullException(nameof(bind));
            }

            var target = new TTarget();
            bind(configuration, target);

            AddNonEmptyDistinct(_knownProxies, ParseValues(target.KnownProxies));
            AddNonEmptyDistinct(_knownNetworks, ParseValues(target.KnownNetworks));
            return this;
        }

        /// <summary>
        /// Adds a forwarded headers options configuration callback.
        /// </summary>
        /// <param name="configure">Configuration callback.</param>
        /// <returns>Current builder instance.</returns>
        public IpSafeForwardedHeadersBuilder WithForwardedHeaders(Action<ForwardedHeadersOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _forwardedHeadersConfigurations.Add(configure);
            return this;
        }

        static void AddNonEmptyDistinct(List<string> target, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!target.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)))
                {
                    target.Add(value);
                }
            }
        }

        static string[] ParseValues(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }
    }
}
