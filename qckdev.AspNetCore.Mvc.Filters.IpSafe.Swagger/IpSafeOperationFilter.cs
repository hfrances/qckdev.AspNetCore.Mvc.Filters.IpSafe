using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger
{
    /// <summary>
    /// Adds the x-ip-safe extension to endpoints protected by <see cref="IpSafeFilterAttribute"/>.
    /// </summary>
    sealed class IpSafeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context == null)
            {
                return;
            }

            var methodInfo = context.MethodInfo;
            var hasIpSafe = HasAttribute<IpSafeFilterAttribute>(methodInfo)
                || HasAttribute<IpSafeFilterAttribute>(methodInfo.DeclaringType);
            if (!hasIpSafe)
            {
                return;
            }

            var allowAnyIp = HasAttribute<AllowAnyIpAddressAttribute>(methodInfo)
                || HasAttribute<AllowAnyIpAddressAttribute>(methodInfo.DeclaringType);
            if (allowAnyIp)
            {
                return;
            }

            operation.Extensions["x-ip-safe"] = new OpenApiBoolean(true);
        }

        static bool HasAttribute<TAttribute>(MemberInfo? memberInfo) where TAttribute : System.Attribute
        {
            if (memberInfo == null)
            {
                return false;
            }

            return memberInfo.GetCustomAttributes(true).OfType<TAttribute>().Any();
        }
    }
}
