
namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{

    /// <summary>
    /// Specifies the settings on for IP request validation.
    /// </summary>
    public sealed class IpSafeListSettings
    {

        /// <summary>
        /// Gets or sets a list of valid IP addresses split by semicolon (;). Null for skip validation by ip address.
        /// </summary>
        public string? IpAddresses { get; set; }
        /// <summary>
        /// Gets or sets a list of valid IP network ranges split by semicolon (;). Null for skip validation by network.
        /// </summary>
        public string? IpNetworks { get; set; }
        /// <summary>
        /// Gets or sets a list of known proxies split by semicolon (;) to accept forwarded headers from. Null for skip validation.
        /// </summary>
        public string? KnownProxies { get; set; } = string.Empty;

    }
}
