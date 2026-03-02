using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// IP Safe settings provider that uses static configuration from IOptions.
    /// This is the default implementation for backward compatibility.
    /// </summary>
    public class IpSafeSettingsProvider : IIpSafeSettingsProvider
    {
        private readonly IOptionsMonitor<IpSafeListSettings> _optionsMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="IpSafeSettingsProvider"/> class.
        /// </summary>
        /// <param name="optionsMonitor">The options monitor.</param>
        /// <exception cref="ArgumentNullException">Thrown when optionsMonitor is null.</exception>
        public IpSafeSettingsProvider(IOptionsMonitor<IpSafeListSettings> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        }

        /// <summary>
        /// Gets the IP Safe settings from IOptions.
        /// </summary>
        public Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IpSafeListSettings?>(_optionsMonitor.CurrentValue);
        }
    }
}
