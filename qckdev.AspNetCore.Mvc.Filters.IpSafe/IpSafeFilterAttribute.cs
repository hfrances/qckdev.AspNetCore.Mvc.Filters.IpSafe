using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <summary>
    /// Specifies that the class or method that this attribute is applied to requires validate IP address during the request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class IpSafeFilterAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Gets the scheme names to resolve settings from.
        /// When empty, default settings are used.
        /// </summary>
        public string[] Schemes { get; }

        /// <summary>
        /// Instantiates a new <see cref="IpSafeFilterAttribute"/> instance.
        /// </summary>
        public IpSafeFilterAttribute() : base(typeof(IpSafeActionFilter))
        {
            this.Schemes = Array.Empty<string>();
        }

        /// <summary>
        /// Instantiates a new <see cref="IpSafeFilterAttribute"/> instance with settings scheme names.
        /// </summary>
        /// <param name="schemes">Scheme names to resolve IP settings from.</param>
        public IpSafeFilterAttribute(params string[] schemes) : base(typeof(IpSafeActionFilter))
        {
            this.Schemes = (schemes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
