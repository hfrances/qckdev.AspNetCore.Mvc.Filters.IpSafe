using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    /// <summary>
    /// Integration tests that validate runtime behavior of IpSafe filter resolution and forwarding scenarios.
    /// </summary>
    [TestClass]
    public class IpSafeIntegrationTests
    {
        /// <summary>
        /// Verifies that loopback traffic to an IpSafe endpoint is accepted when no forwarded IP is provided.
        /// Expected result: HTTP 200 OK.
        /// </summary>
        [TestMethod]
        public void ProtectedEndpoint_WithoutForwardedHeader_AllowsLoopback()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };

            var response = client.GetAsync("ipsafe/protected").GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verifies that a non-allowed forwarded IP is rejected on an IpSafe endpoint.
        /// Expected result: HTTP 403 Forbidden.
        /// </summary>
        [TestMethod]
        public void ProtectedEndpoint_WithNotAllowedForwardedFor_ReturnsForbidden()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/protected");
            request.Headers.Add("X-Forwarded-For", "8.8.8.8");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Verifies that endpoint-level <see cref="AllowAnyIpAddressAttribute"/> bypasses controller-level IpSafe.
        /// Expected result: HTTP 200 OK.
        /// </summary>
        [TestMethod]
        public void PublicEndpoint_WithNotAllowedForwardedFor_AllowsByAttribute()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/public");
            request.Headers.Add("X-Forwarded-For", "8.8.8.8");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verifies endpoint precedence: endpoint-level <see cref="IpSafeFilterAttribute"/> overrides controller AllowAny.
        /// Expected result: HTTP 403 Forbidden.
        /// </summary>
        [TestMethod]
        public void EndpointWithIpSafeUnderAllowAnyController_WithNotAllowedForwardedFor_ReturnsForbidden()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe-allowany-controller/protected-endpoint");
            request.Headers.Add("X-Forwarded-For", "8.8.8.8");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Verifies that when controller has both attributes and endpoint has none, AllowAny wins at controller level.
        /// Expected result: HTTP 200 OK.
        /// </summary>
        [TestMethod]
        public void DefaultEndpointUnderControllerWithBothAttributes_WithNotAllowedForwardedFor_ReturnsOk()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe-both-controller/default");
            request.Headers.Add("X-Forwarded-For", "8.8.8.8");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verifies endpoint precedence over controller both-state: endpoint IpSafe enforces IP restrictions.
        /// Expected result: HTTP 403 Forbidden.
        /// </summary>
        [TestMethod]
        public void EndpointIpSafeUnderControllerWithBothAttributes_WithNotAllowedForwardedFor_ReturnsForbidden()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe-both-controller/endpoint-ip-safe");
            request.Headers.Add("X-Forwarded-For", "8.8.8.8");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Verifies that when endpoint declares both attributes, AllowAny wins at endpoint level.
        /// Expected result: HTTP 200 OK.
        /// </summary>
        [TestMethod]
        public void EndpointWithBothAttributes_WithNotAllowedForwardedFor_ReturnsOk()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/endpoint-both-attributes");
            request.Headers.Add("X-Forwarded-For", "8.8.8.8");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verifies that endpoint IpSafe using a named scheme resolves that scheme settings.
        /// Expected result: HTTP 200 OK for an IP included in the Internal scheme.
        /// </summary>
        [TestMethod]
        public void SchemeEndpoint_WithInternalSchemeIp_ReturnsOk()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/scheme-internal");
            request.Headers.Add("X-Forwarded-For", "203.0.113.10");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verifies that endpoint IpSafe using a named scheme rejects addresses outside that scheme.
        /// Expected result: HTTP 403 Forbidden for non-listed IP.
        /// </summary>
        [TestMethod]
        public void SchemeEndpoint_WithIpOutsideInternalScheme_ReturnsForbidden()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/scheme-internal");
            request.Headers.Add("X-Forwarded-For", "198.51.100.20");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Verifies that multiple schemes in one endpoint are evaluated with OR semantics.
        /// Expected result: HTTP 200 OK when IP matches any configured scheme.
        /// </summary>
        [TestMethod]
        public void MultiSchemeEndpoint_WithPartnerSchemeIp_ReturnsOk()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };
            using var request = new HttpRequestMessage(HttpMethod.Get, "ipsafe/scheme-internal-or-partner");
            request.Headers.Add("X-Forwarded-For", "198.51.100.20");
            request.Headers.Add("X-Forwarded-Proto", "http");

            var response = client.SendAsync(request).GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
