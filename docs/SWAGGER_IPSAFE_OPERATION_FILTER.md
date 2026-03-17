# Swagger IpSafe Operation Filter

This document explains how `IpSafeOperationFilter` decides whether to add `x-ip-safe: true` to an OpenAPI operation.

## Purpose

`IpSafeOperationFilter` is part of `qckdev.AspNetCore.Mvc.Filters.IpSafe.Swagger`.

It inspects controller and endpoint attributes:
- `IpSafeFilterAttribute`
- `AllowAnyIpAddressAttribute`

Then it adds:
- `x-ip-safe: true` when the operation is considered IP-protected.

## Decision Rule

The implementation uses this logic:

1. Evaluate endpoint attributes first:
   - endpoint `AllowAnyIpAddress` => do not add `x-ip-safe`
   - endpoint `IpSafeFilter` => add `x-ip-safe`
2. If endpoint has none of both attributes, evaluate controller attributes:
   - controller `AllowAnyIpAddress` => do not add `x-ip-safe`
   - controller `IpSafeFilter` => add `x-ip-safe`
3. If neither level has those attributes, do not add `x-ip-safe`.

## Combination Matrix

Controller states are columns. Endpoint states are rows.

Legend:
- `None`: no `IpSafeFilter`, no `AllowAnyIpAddress`
- `IpSafe`: `IpSafeFilter` only
- `AllowAny`: `AllowAnyIpAddress` only
- `Both`: `IpSafeFilter` + `AllowAnyIpAddress`

| Endpoint \ Controller | None | IpSafe | AllowAny | Both |
|---|---|---|---|---|
| None | ❌ | ✅ | ❌ | ❌ |
| IpSafe | ✅ | ✅ | ✅ | ✅ |
| AllowAny | ❌ | ❌ | ❌ | ❌ |
| Both | ❌ | ❌ | ❌ | ❌ |

Result legend:
- ✅ = Checks Ip
- ❌ = Allows any

## Notes

- Endpoint level has precedence over controller level.
- At the same level, `AllowAnyIpAddress` has precedence over `IpSafeFilter`.
- `x-ip-safe` is an OpenAPI extension used by Swagger UI customization only.
- It does not change runtime filtering behavior; runtime behavior is still enforced by `IpSafeFilter`.
