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

        [HttpGet("endpoint-both-attributes")]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.AllowAnyIpAddress]
        public IActionResult EndpointBothAttributes()
        {
            return Ok("endpoint-both-ok");
        }

        [HttpGet("scheme-internal")]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter("Internal")]
        public IActionResult SchemeInternal()
        {
            return Ok("scheme-internal-ok");
        }

        [HttpGet("scheme-internal-or-partner")]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter("Internal", "Partner")]
        public IActionResult SchemeInternalOrPartner()
        {
            return Ok("scheme-internal-or-partner-ok");
        }
    }

    [ApiController]
    [Route("ipsafe-allowany-controller")]
    [qckdev.AspNetCore.Mvc.Filters.IpSafe.AllowAnyIpAddress]
    public sealed class IpSafeAllowAnyController : ControllerBase
    {
        [HttpGet("protected-endpoint")]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter]
        public IActionResult ProtectedEndpoint()
        {
            return Ok("protected-endpoint-ok");
        }
    }

    [ApiController]
    [Route("ipsafe-both-controller")]
    [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter]
    [qckdev.AspNetCore.Mvc.Filters.IpSafe.AllowAnyIpAddress]
    public sealed class IpSafeBothAttributesController : ControllerBase
    {
        [HttpGet("default")]
        public IActionResult Default()
        {
            return Ok("both-controller-default-ok");
        }

        [HttpGet("endpoint-ip-safe")]
        [qckdev.AspNetCore.Mvc.Filters.IpSafe.IpSafeFilter]
        public IActionResult EndpointIpSafe()
        {
            return Ok("both-controller-endpoint-ipsafe-ok");
        }
    }
}
