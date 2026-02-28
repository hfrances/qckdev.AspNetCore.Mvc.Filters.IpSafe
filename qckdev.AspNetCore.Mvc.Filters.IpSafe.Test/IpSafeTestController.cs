using Microsoft.AspNetCore.Mvc;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    [ApiController]
    [Route("ipsafe")]
    [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter]
    public sealed class IpSafeTestController : ControllerBase
    {
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            return Ok("protected-ok");
        }

        [HttpGet("public")]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.AllowAnyIpAddress]
        public IActionResult Public()
        {
            return Ok("public-ok");
        }
    }
}
