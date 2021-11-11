using Microsoft.AspNetCore.Mvc;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    public sealed class IpSafeFilterAttribute : TypeFilterAttribute
    {

        public IpSafeFilterAttribute() : base(typeof(IpSafeActionFilter))
        {

        }

    }
}
