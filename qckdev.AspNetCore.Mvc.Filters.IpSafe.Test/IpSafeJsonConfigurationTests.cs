using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    [TestClass]
    public class IpSafeJsonConfigurationTests
    {
        [DataTestMethod]
        [DataRow("null", "null", "203.0.113.20", false)]
        [DataRow("\"\"", "\"\"", "203.0.113.20", false)]
        [DataRow("[]", "[]", "203.0.113.20", false)]
        [DataRow("\"203.0.113.10\"", "null", "203.0.113.10", false)]
        [DataRow("\"203.0.113.10\"", "null", "203.0.113.20", true)]
        [DataRow("null", "\"198.51.100.0/24\"", "198.51.100.20", false)]
        [DataRow("null", "\"198.51.100.0/24\"", "203.0.113.20", true)]
        public void JsonLists_FilterUsesRemainingRulesOrAllowsWhenBothEmpty(
            string addressesJson, string networksJson, string remoteIp, bool forbidden)
        {
            var json = "{\"Security\":{\"IpSafeList\":{\"IpAddresses\":" + addressesJson
                + ",\"IpNetworks\":" + networksJson + ",\"KnownProxies\":null}}}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIpSafeFilter<IpSafeSettingsProvider>(options =>
                options.WithConfiguration(configuration, "Security:IpSafeList"));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var attribute = new IpSafeFilterAttribute();
            var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            var actionArguments = new Dictionary<string, object?>();
            var context = new ActionExecutingContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata> { attribute },
                actionArguments,
                new object());
            var filter = (IActionFilter)attribute.CreateInstance(scope.ServiceProvider);

            filter.OnActionExecuting(context);

            if (forbidden)
            {
                Assert.IsInstanceOfType(context.Result, typeof(StatusCodeResult));
                Assert.AreEqual(StatusCodes.Status403Forbidden, ((StatusCodeResult)context.Result!).StatusCode);
            }
            else
            {
                Assert.IsNull(context.Result);
            }
        }
    }
}
