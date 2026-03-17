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
        public string? KnownProxies { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of trusted networks split by semicolon (;).
        /// </summary>
        public string? KnownNetworks { get; set; }
    }
}
