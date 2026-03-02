namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Advanced options for IP Safe filter with support for custom settings providers.
    /// </summary>
    public class IpSafeOptions
    {
        /// <summary>
        /// Gets or sets the IP Safe settings values when using inline configuration.
        /// </summary>
        public IpSafeListSettings? Settings { get; set; }
    }
}
