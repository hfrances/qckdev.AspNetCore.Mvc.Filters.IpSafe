# Integration Testing

## Overview

This project includes comprehensive integration tests that validate IP-based access control filtering in a realistic ASP.NET Core context.

## Test Architecture

The tests spin up a minimal ASP.NET Core host for isolation and validation:

1. **LocalTestServiceManager** - Hosts ASP.NET Core application on dynamic port (25000 + PID)
2. **TestAssemblySetup** - Manages host lifecycle (startup/shutdown)
3. **IpSafeTestController** - Provides protected and unrestricted endpoints
4. **IpSafeIntegrationTests** - Validates IP filtering behavior

### Why Integration Tests?

Integration tests validate the **complete IP filtering pipeline** including:
- ✅ Loopback address handling
- ✅ X-Forwarded-For header parsing
- ✅ Known proxy validation
- ✅ Forward limit enforcement
- ✅ Per-endpoint attribute overrides
- ✅ HTTP status codes and error responses

This catches issues that unit tests cannot: header precedence, proxy chain handling, attribute scope, etc.

## Test Coverage

### Integration Tests (3 tests)

#### Loopback Access (Direct Connection)

**ProtectedEndpoint_WithoutForwardedHeader_AllowsLoopback**
```csharp
GET /ipsafe/protected
Host: localhost
Remote IP: 127.0.0.1
// No X-Forwarded-For header

Expected: HTTP 200 OK
Response: "protected-ok"
```
Validates that direct loopback connections are allowed without requiring specific IP configuration.

#### Forwarded Header Filtering

**ProtectedEndpoint_WithNotAllowedForwardedFor_ReturnsForbidden**
```csharp
GET /ipsafe/protected
Host: localhost
X-Forwarded-For: 8.8.8.8
X-Forwarded-Proto: http

Expected: HTTP 403 Forbidden
```
Validates that forwarded requests from non-allowed IPs are rejected with 403 Forbidden status.

#### Per-Endpoint Override Attribute

**PublicEndpoint_WithNotAllowedForwardedFor_AllowsByAttribute**
```csharp
GET /ipsafe/public
Host: localhost
X-Forwarded-For: 8.8.8.8
X-Forwarded-Proto: http
// Endpoint decorated with [AllowAnyIpAddress]

Expected: HTTP 200 OK
Response: "public-ok"
```
Validates that `[AllowAnyIpAddress]` attribute disables IP filtering on per-endpoint basis.

## Test Configuration

### Test Environment

| Setting | Value |
|---------|-------|
| **Service Port** | Dynamic (25000 + Process ID) |
| **Allowed IPs** | 127.0.0.1 (IPv4), ::1 (IPv6) |
| **Known Proxies** | Loopback addresses (127.0.0.1, ::1) |
| **Forward Limit** | 1 hop maximum |
| **Headers** | X-Forwarded-For, X-Forwarded-Proto |

### Forwarded Headers Configuration

The test service configures ASP.NET Core's middleware to process forwarded headers:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1,
    KnownProxies = { IPAddress.Loopback, IPAddress.IPv6Loopback }
});
```

## Running Tests

### Run All Tests

```bash
cd qckdev.AspNetCore.Mvc.Filters.IpSafe
dotnet test qckdev.AspNetCore.Mvc.Filters.IpSafe.sln -v minimal
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~IpSafeIntegrationTests.ProtectedEndpoint_WithoutForwardedHeader"
```

### Run with Detailed Logging

```bash
dotnet test --logger "console;verbosity=detailed" --no-build
```

## Test Execution Flow

```
[AssemblyInitialize]
  ↓
  LocalTestServiceManager.StartIfNeeded()
    ├─ Create IHostBuilder
    ├─ Configure services (AddControllers, AddIpSafeFilter)
    ├─ Configure middleware (UseForwardedHeaders, UseIpSafeFilter)
    ├─ Start host on dynamic port
    └─ Wait for service ready (polling, 10s timeout)
  ↓
[Test Methods Execute]
  ├─ Create HttpClient with base address from service
  ├─ Build request with headers (X-Forwarded-For, etc.)
  ├─ Send request to live endpoint
  └─ Assert response status and behavior
  ↓
[AssemblyCleanup]
  ↓
  LocalTestServiceManager.Stop()
    └─ Dispose host
```

## Multi-Framework Coverage

Tests execute against all supported frameworks:

- .NET Standard 2.0 (compatibility)
- .NET Core 3.1 (EOL December 2022)
- .NET 5.0 (EOL May 2022)
- .NET 6.0 (LTS until November 2024)
- .NET 8.0 (LTS until November 2026)
- .NET 10.0 (Current, supported until November 2027)

Each framework runs **3 integration tests** independently.

## Key Validations

✅ **Loopback handling** - 127.0.0.1 and ::1 always allowed  
✅ **Forwarded header parsing** - X-Forwarded-For header recognized  
✅ **Known proxy validation** - Forwarded headers only accepted from known proxies  
✅ **Forward limit enforcement** - Maximum 1 hop from known proxy  
✅ **Per-endpoint override** - `[AllowAnyIpAddress]` attribute works correctly  
✅ **HTTP status codes** - 403 Forbidden for rejected requests  
✅ **Error responses** - Graceful handling of malformed headers  

## IP Whitelist Behavior

### Loopback Addresses (Always Allowed)

- `127.0.0.1` - IPv4 loopback
- `::1` - IPv6 loopback

These addresses are always allowed regardless of configuration.

### Configured Allowed IPs

In this test, the allowed IPs are:
- (Loopback addresses only)

To extend allowed IPs, configure in `Startup.cs`:

```csharp
services.AddIpSafeFilter(new IpSafeListSettings
{
    IpAddresses = "127.0.0.1;::1;192.168.1.100",
    IpNetworks = "192.168.0.0/24;10.0.0.0/8"
});
```

### Forwarded Requests

When requests come through a proxy:

1. Client → Proxy: Proxy adds `X-Forwarded-For: client-ip`
2. Proxy → Server: Server must trust proxy (in KnownProxies)
3. Server validates: `client-ip` must be in allowed list

## Common Use Cases

### Allow Company Network

```csharp
services.AddIpSafeFilter(new IpSafeListSettings
{
    IpAddresses = "127.0.0.1;::1",
    IpNetworks = "192.168.1.0/24;10.0.0.0/8"
});
```

### Allow Azure App Service

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor,
    KnownProxies = { IPAddress.Parse("168.63.129.16") }  // Azure proxy
});
```

### Allow Specific Partner

```csharp
services.AddIpSafeFilter(new IpSafeListSettings
{
    IpAddresses = "203.0.113.42"  // Partner VPN gateway
});
```

## Troubleshooting

### Test Hangs on Service Ready

**Symptom**: Test hangs for 10 seconds then fails  
**Cause**: Service not responding on expected port  
**Solution**: 
- Verify port is not in use
- Check network settings allow loopback
- Review firewall configuration

### Forwarded Headers Not Processed

**Symptom**: X-Forwarded-For header ignored, connection fails  
**Cause**: Proxy not in KnownProxies list  
**Solution**:
- Add proxy IP to KnownProxies in ForwardedHeadersOptions
- Verify X-Forwarded-For header syntax is valid
- Check ForwardLimit doesn't exceed proxy chain depth

### All Requests Blocked

**Symptom**: Even localhost requests return 403 Forbidden  
**Cause**: Allowed IPs list is empty or misconfigured  
**Solution**:
- Verify IpAddresses includes "127.0.0.1;::1"
- Check IpNetworks syntax (CIDR notation required)
- Enable debug logging to see evaluated IP addresses

### Attribute Not Working

**Symptom**: `[AllowAnyIpAddress]` doesn't bypass filtering  
**Cause**: Filter not checking attribute, or attribute applied wrong  
**Solution**:
- Verify attribute is on action method (not controller)
- Check filter registration order in middleware
- Use debugger to verify attribute presence

## Best Practices

1. **Principle of Least Privilege** - Only allow required IPs
2. **Document Whitelist** - Maintain list of why each IP is allowed
3. **Monitor Rejections** - Log 403 Forbidden responses for analysis
4. **Test Overrides** - Verify `[AllowAnyIpAddress]` works as expected
5. **Proxy Configuration** - Keep proxy list in configuration, not hardcoded
6. **IPv6 Support** - Test both IPv4 and IPv6 addresses
7. **Header Validation** - Ensure X-Forwarded-Proto matches your URL scheme

## Security Considerations

### Trusting Forwarded Headers

⚠️ **Only trust X-Forwarded-For when proxy is in KnownProxies**

If you trust any proxy:
```csharp
// DANGEROUS - accepts spoofed headers
options.KnownProxies.Clear();
options.KnownNetworks.Clear();
```

Safe configuration:
```csharp
// SAFE - only trusts specific proxies
options.KnownProxies.Clear();
options.KnownProxies.Add(IPAddress.Parse("proxy.example.com"));
```

### Forward Limit

The forward limit protects against proxy chain injection:

```csharp
// Process max 1 header from each proxy
// Rejects: X-Forwarded-For: spoofed-ip, real-client-ip
options.ForwardLimit = 1;
```

### Logging

Always log IP-based rejections:

```csharp
app.UseIpSafeFilter(options =>
{
    // Configure...
});
```

## Related Documentation

- [Test Coverage Guide](../../TEST_COVERAGE.md) - Coverage across all projects
- [Framework Compatibility](COMPATIBILITY.md) - Supported frameworks and versions
- [Main README](../README.md) - Usage examples and configuration

---

Last updated: March 1, 2026
