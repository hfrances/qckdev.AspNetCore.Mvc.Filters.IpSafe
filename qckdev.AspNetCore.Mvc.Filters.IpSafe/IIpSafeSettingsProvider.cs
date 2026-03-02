using System.Threading;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Provides the contract for retrieving IP Safe settings from custom sources.
    /// Enables flexible configuration strategies (database, API, configuration, cache, etc.)
    /// </summary>
    public interface IIpSafeSettingsProvider
    {
        /// <summary>
        /// Gets the IP Safe settings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The IP Safe settings, or null if not configured.</returns>
        Task<IpSafeListSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
    }
}
