using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    [TestClass]
    public class IpSafeIntegrationTests
    {
        [TestMethod]
        public void ProtectedEndpoint_WithoutForwardedHeader_AllowsLoopback()
        {
            using var client = new HttpClient { BaseAddress = LocalTestServiceManager.ServiceUri };

            var response = client.GetAsync("ipsafe/protected").GetAwaiter().GetResult();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

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
    }
}
