using System.Threading;
using System.Threading.Tasks;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Provides trusted proxy and trusted network settings for forwarded headers processing.
    /// </summary>
    public interface IIpSafeTrustedProxiesProvider
    {
        /// <summary>
        /// Gets trusted proxies/networks settings.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Trusted proxies/networks settings, or null when not configured.</returns>
        Task<IpSafeTrustedProxiesSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
    }
}
