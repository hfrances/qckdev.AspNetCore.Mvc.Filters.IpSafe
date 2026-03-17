# Documentation

Welcome to the documentation for `qckdev.AspNetCore.Mvc.Filters.IpSafe`.

## Table of Contents

- [Framework Compatibility](COMPATIBILITY.md) - Supported frameworks, package versions, and security considerations
- [Integration Testing](TESTING.md) - Comprehensive test suite for IP Safe filter
- [Swagger IpSafe Operation Filter](SWAGGER_IPSAFE_OPERATION_FILTER.md) - Attribute evaluation rules and detection matrix for `x-ip-safe`
- [Test Dependencies Quick Reference](TEST_DEPENDENCIES_QUICK_REFERENCE.md) - Checklist for multi-framework test changes
- [Test Dependencies Update](TEST_DEPENDENCIES_UPDATE.md) - Comprehensive rules for editing test projects
- [Test Coverage Guide](../TEST_COVERAGE.md) - Patterns and coverage across all projects

## Quick Links

- [Main README](../README.md)
- [NuGet Package](https://www.nuget.org/packages/qckdev.AspNetCore.Mvc.Filters.IpSafe)
- [GitHub Repository](https://github.com/hfrances/qckdev.AspNetCore.Mvc.Filters.IpSafe)

## Documentation Contents

### [Framework Compatibility](COMPATIBILITY.md)
Detailed information about:
- Supported .NET frameworks and their status
- Package version mapping per framework
- Security considerations and recommendations
- Package version analysis

### [Integration Testing](TESTING.md)
Detailed guide to:
- Integration test architecture and patterns
- Complete test coverage (3 tests)
- IP whitelisting behavior
- Forwarded headers configuration
- Troubleshooting and best practices

### [Swagger IpSafe Operation Filter](SWAGGER_IPSAFE_OPERATION_FILTER.md)
Detailed information about:
- `IpSafeOperationFilter` purpose and scope
- Exact rule used to emit `x-ip-safe`
- Full Controller/Endpoint attribute combination matrix
- Precedence of `AllowAnyIpAddress` over `IpSafeFilter`

### [Test Dependencies - Layered Model](TEST_DEPENDENCIES_QUICK_REFERENCE.md)

**Capa 1 (base):** Checklist for safe multi-framework test changes
- [Test Dependencies Quick Reference](TEST_DEPENDENCIES_QUICK_REFERENCE.md)

**Capa 2 (detailed):** Complete rules for editing test projects
- [Test Dependencies Update](TEST_DEPENDENCIES_UPDATE.md)

## Scope

These guides cover:
- `qckdev.AspNetCore.Mvc.Filters.IpSafe`
- `qckdev.AspNetCore.Mvc.Filters.IpSafe.Test`

---

Last updated: March 17, 2026
