using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.IO;

namespace qckdev.AspNetCore.Mvc.Filters.IpSafe.Test
{
    /// <summary>
    /// Verifies that Swagger marks operations with <c>x-ip-safe</c> according to IpSafe/AllowAny attribute precedence.
    /// </summary>
    [TestClass]
    public class SwaggerIpSafeOperationFilterTests
    {
        /// <summary>
        /// Ensures that an endpoint decorated with <see cref="IpSafeFilterAttribute"/> is marked as IP protected.
        /// Expected result: <c>x-ip-safe</c> exists.
        /// </summary>
        [TestMethod]
        public void EndpointWithIpSafeFilter_AddsXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/endpoint-level", OperationType.Get);
            Assert.IsTrue(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures that controller-level <see cref="IpSafeFilterAttribute"/> applies to endpoints without local override.
        /// Expected result: <c>x-ip-safe</c> exists.
        /// </summary>
        [TestMethod]
        public void ControllerWithIpSafeFilter_AddsXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/controller-level", OperationType.Get);
            Assert.IsTrue(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures that when controller has both attributes, AllowAny wins at controller level.
        /// Expected result: <c>x-ip-safe</c> does not exist.
        /// </summary>
        [TestMethod]
        public void ControllerWithIpSafeAndAllowAnyIp_DoesNotAddXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/controller-level-allow-any", OperationType.Get);
            Assert.IsFalse(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures endpoint precedence over controller: endpoint IpSafe overrides controller AllowAny.
        /// Expected result: <c>x-ip-safe</c> exists.
        /// </summary>
        [TestMethod]
        public void ControllerWithAllowAnyIpAndEndpointIpSafe_AddsXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/endpoint-level-under-controller-allow-any", OperationType.Get);
            Assert.IsTrue(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures endpoint precedence over controller: endpoint AllowAny overrides controller IpSafe.
        /// Expected result: <c>x-ip-safe</c> does not exist.
        /// </summary>
        [TestMethod]
        public void ControllerWithIpSafeAndEndpointAllowAnyIp_DoesNotAddXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/endpoint-allow-any-under-controller-ip-safe", OperationType.Get);
            Assert.IsFalse(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures that when both attributes are present at endpoint level, AllowAny wins.
        /// Expected result: <c>x-ip-safe</c> does not exist.
        /// </summary>
        [TestMethod]
        public void EndpointWithIpSafeAndAllowAnyIp_DoesNotAddXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/endpoint-with-both-attributes", OperationType.Get);
            Assert.IsFalse(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures that endpoints without IpSafe attributes are not marked as IP protected.
        /// Expected result: <c>x-ip-safe</c> does not exist.
        /// </summary>
        [TestMethod]
        public void EndpointWithoutIpSafe_DoesNotAddXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/no-ip-safe", OperationType.Get);
            Assert.IsFalse(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        /// <summary>
        /// Ensures that HTTP verb does not affect detection when endpoint has <see cref="IpSafeFilterAttribute"/>.
        /// Expected result: <c>x-ip-safe</c> exists for POST operation.
        /// </summary>
        [TestMethod]
        public void PostEndpointWithIpSafe_AddsXIpSafeExtension()
        {
            var operation = GetOperation("/swagger-ip-safe/endpoint-level-post", OperationType.Post);
            Assert.IsTrue(operation.Extensions.ContainsKey("x-ip-safe"));
        }

        static OpenApiOperation GetOperation(string path, OperationType method)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
            services
                .AddControllers()
                .AddApplicationPart(typeof(SwaggerIpSafeEndpointLevelController).Assembly);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "test", Version = "v1" });
                c.AddIpSafeSwagger();
            });

            using var provider = services.BuildServiceProvider();
            var swaggerProvider = provider.GetRequiredService<ISwaggerProvider>();
            var document = swaggerProvider.GetSwagger("v1");
            var pathItem = document.Paths[path];
            return pathItem.Operations[method];
        }

        sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = typeof(SwaggerIpSafeOperationFilterTests).Assembly.GetName().Name!;
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string WebRootPath { get; set; } = string.Empty;
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }

    [ApiController]
    [Route("swagger-ip-safe")]
    public sealed class SwaggerIpSafeEndpointLevelController : ControllerBase
    {
        [HttpGet("endpoint-level")]
        [IpSafeFilter]
        public ActionResult<string> Get()
            => Ok("ok");

        [HttpGet("endpoint-with-both-attributes")]
        [IpSafeFilter]
        [AllowAnyIpAddress]
        public ActionResult<string> GetBoth()
            => Ok("ok");

        [HttpGet("no-ip-safe")]
        public ActionResult<string> GetNoIpSafe()
            => Ok("ok");

        [HttpPost("endpoint-level-post")]
        [IpSafeFilter]
        public ActionResult<string> Post()
            => Ok("ok");
    }

    [ApiController]
    [Route("swagger-ip-safe")]
    [IpSafeFilter]
    public sealed class SwaggerIpSafeControllerLevelController : ControllerBase
    {
        [HttpGet("controller-level")]
        public ActionResult<string> Get()
            => Ok("ok");

        [HttpGet("endpoint-allow-any-under-controller-ip-safe")]
        [AllowAnyIpAddress]
        public ActionResult<string> GetAllowAny()
            => Ok("ok");
    }

    [ApiController]
    [Route("swagger-ip-safe")]
    [IpSafeFilter]
    [AllowAnyIpAddress]
    public sealed class SwaggerIpSafeControllerLevelAllowAnyController : ControllerBase
    {
        [HttpGet("controller-level-allow-any")]
        public ActionResult<string> Get()
            => Ok("ok");
    }

    [ApiController]
    [Route("swagger-ip-safe")]
    [AllowAnyIpAddress]
    public sealed class SwaggerIpSafeControllerAllowAnyEndpointIpSafeController : ControllerBase
    {
        [HttpGet("endpoint-level-under-controller-allow-any")]
        [IpSafeFilter]
        public ActionResult<string> Get()
            => Ok("ok");
    }
}
