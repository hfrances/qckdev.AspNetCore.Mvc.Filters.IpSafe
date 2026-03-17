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
            var endpointDecision = GetLocalDecision(methodInfo);
            if (endpointDecision.HasValue)
            {
                if (endpointDecision.Value)
                {
                    operation.Extensions["x-ip-safe"] = new OpenApiBoolean(true);
                }
                return;
            }

            var controllerDecision = GetLocalDecision(methodInfo.DeclaringType);
            if (controllerDecision.HasValue && controllerDecision.Value)
            {
                operation.Extensions["x-ip-safe"] = new OpenApiBoolean(true);
            }
        }

        static bool? GetLocalDecision(MemberInfo? memberInfo)
        {
            if (memberInfo == null)
            {
                return null;
            }

            if (HasAttribute<AllowAnyIpAddressAttribute>(memberInfo))
            {
                return false;
            }

            if (HasAttribute<IpSafeFilterAttribute>(memberInfo))
            {
                return true;
            }

            return null;
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
