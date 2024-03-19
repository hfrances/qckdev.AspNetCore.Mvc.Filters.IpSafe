using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe
{
    /// <remarks>
    /// https://code-maze.com/filters-in-asp-net-core-mvc/
    /// https://code-maze.com/action-filters-aspnetcore/
    /// </remarks>
    sealed class IpSafeActionFilter : IActionFilter
    {
        IOptions<IpSafeListSettings> IpSafeListSettings { get; }


        public IpSafeActionFilter(IOptions<IpSafeListSettings> ipSafeListSettings)
        {
            this.IpSafeListSettings = ipSafeListSettings;
        }

        public void OnAuthorization(AuthorizationFilterContext _)
        {
            // Do Nothing;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            Validate(context);
        }

        public void OnActionExecuted(ActionExecutedContext _)
        {
            // Do Nothing.
        }

        private void Validate(ActionExecutingContext context)
        {

            var ipAddresses =
                IpSafeListSettings.Value.IpAddresses?
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(IPAddress.Parse)
                ?? Array.Empty<IPAddress>();
            var ipNetworks =
                IpSafeListSettings.Value.IpNetworks?
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(IPNetwork.Parse)
                ?? Array.Empty<IPNetwork>();
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
            var allowAny = context.Filters.OfType<AllowAnyIpAddressAttribute>().Any();

            if (remoteIp == null)
            {
                throw new ArgumentException("Remote IP is NULL, may due to missing ForwardedHeaders.");
            }
            else if (allowAny)
            {
                // Do nothing. AllowAnyIp attribute set.
            }
            else
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    remoteIp = remoteIp.MapToIPv4();
                }

                if (ipAddresses.Any() || ipNetworks.Any())
                {
                    if (!ipAddresses.Contains(remoteIp) && !ipNetworks.Any(x => x.Contains(remoteIp)))
                    {
                        context.Result = new UnauthorizedResult();
                    }
                }
                else
                {
                    // Do Nothing. No restrictions defined.
                }
            }
        }

    }
}
