using Microsoft.AspNetCore.Builder;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Allows a settings object to configure forwarded headers options directly.
    /// </summary>
    public interface IIpSafeForwardedHeadersSource
    {
        /// <summary>
        /// Applies forwarded headers configuration to the specified options instance.
        /// </summary>
        /// <param name="options">The forwarded headers options to configure.</param>
        void ConfigureForwardedHeaders(ForwardedHeadersOptions options);
    }
}
