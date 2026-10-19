using qckdev.AspNetCore.ForwardedHeaders;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Settings used to configure trusted proxies/networks for forwarded headers.
    /// </summary>
    public sealed class IpSafeTrustedProxiesSettings : IIpSafeForwardedHeadersTarget
    {
        /// <summary>
        /// Gets or sets a list of trusted proxies split by semicolon (;).
        /// </summary>
        public DelimitedStringList? KnownProxies { get; set; } = new DelimitedStringList();

        /// <summary>
        /// Gets or sets a list of trusted networks split by semicolon (;).
        /// </summary>
        public DelimitedStringList? KnownNetworks { get; set; }
    }
}
