using Microsoft.AspNetCore.Mvc;
using qckdev.AspNetCore.Mvc.Filters.IpSafe;

namespace IpSafeExample.DockerProxy.Controllers
{
    [ApiController]
    [Route("ipsafe"), IpSafeFilter]
    public class IpSafeController : ControllerBase
    {
        /// <summary>Accepts requests from any client IP.</summary>
        [HttpGet("public"), AllowAnyIpAddress]
        public IActionResult Public() => Ok(new
        {
            endpoint = "public",
            clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        /// <summary>Accepts client IPs in the Office scheme.</summary>
        /// <response code="403">The client IP is outside the Office scheme.</response>
        [HttpGet("office"), IpSafeFilter(IpSafeSchemes.Office)]
        public IActionResult Office() => Ok(new
        {
            endpoint = "office",
            clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        /// <summary>Accepts client IPs in the Partner scheme.</summary>
        /// <response code="403">The client IP is outside the Partner scheme.</response>
        [HttpGet("partner"), IpSafeFilter(IpSafeSchemes.Partner)]
        public IActionResult Partner() => Ok(new
        {
            endpoint = "partner",
            clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
    }
}
