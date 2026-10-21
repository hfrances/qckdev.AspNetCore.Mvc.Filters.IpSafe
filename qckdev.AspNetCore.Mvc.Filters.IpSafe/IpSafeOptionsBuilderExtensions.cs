using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Configuration helpers for default and named IpSafe options.
    /// </summary>
    public static class IpSafeOptionsBuilderExtensions
    {
        /// <summary>
        /// Binds an IpSafe configuration section to this builder's options name.
        /// </summary>
        /// <param name="builder">The options builder.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="sectionName">The IpSafe settings section.</param>
        /// <returns>The options builder.</returns>
        public static OptionsBuilder<IpSafeListSettings> WithConfiguration(
            this OptionsBuilder<IpSafeListSettings> builder,
            IConfiguration configuration,
            string sectionName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentException("Section name cannot be null or empty.", nameof(sectionName));
            }

            return builder.Bind(configuration.GetSection(sectionName));
        }
    }
}
