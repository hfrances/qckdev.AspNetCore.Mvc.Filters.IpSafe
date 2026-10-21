using qckdev.AspNetCore.ForwardedHeaders;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Contract for configuration targets that can provide known proxies and known networks values.
    /// </summary>
    public interface IIpSafeForwardedHeadersTarget
    {
        /// <summary>
        /// Gets or sets trusted proxies split by semicolon (;).
        /// </summary>
        DelimitedStringList? KnownProxies { get; set; }

        /// <summary>
        /// Gets or sets trusted networks split by semicolon (;).
        /// </summary>
        DelimitedStringList? KnownNetworks { get; set; }
    }
}
