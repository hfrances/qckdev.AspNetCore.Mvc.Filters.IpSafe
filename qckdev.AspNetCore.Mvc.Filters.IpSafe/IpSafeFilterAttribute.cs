using Microsoft.AspNetCore.Mvc;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{

    /// <summary>
    /// Specifies that the class or method that this attribute is applied to requires validate IP address during the request.
    /// </summary>
    public sealed class IpSafeFilterAttribute : TypeFilterAttribute
    {

        /// <summary>
        /// Instantiates a new <see cref="IpSafeFilterAttribute"/> instance.
        /// </summary>
        public IpSafeFilterAttribute() : base(typeof(IpSafeActionFilter))
        {

        }

    }
}
