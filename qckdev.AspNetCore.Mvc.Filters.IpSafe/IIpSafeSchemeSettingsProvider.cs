using System.Threading;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Extends <see cref="IIpSafeSettingsProvider"/> with named-scheme settings resolution.
    /// </summary>
    public interface IIpSafeSchemeSettingsProvider : IIpSafeSettingsProvider
    {
        /// <summary>
        /// Gets the IP Safe settings for a specific scheme.
        /// </summary>
        /// <param name="scheme">Scheme name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The settings for the requested scheme, or null if not configured.</returns>
        Task<IpSafeListSettings?> GetSettingsAsync(string scheme, CancellationToken cancellationToken = default);
    }
}
