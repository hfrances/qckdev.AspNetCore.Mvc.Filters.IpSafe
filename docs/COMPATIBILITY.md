# Framework Compatibility

This document describes the framework compatibility and security considerations for `qckdev.AspNetCore.Mvc.Filters.IpSafe`.

## Supported Frameworks

This package supports the following target frameworks:

| Framework | Status | Support Level |
|-----------|--------|---------------|
| .NET Standard 2.0 | ✅ Supported | Legacy compatibility (.NET Framework 4.7.2+, .NET Core 2.0+) |
| .NET 6.0 | ✅ Supported | LTS - End of Life November 2024 |
| .NET 8.0 | ✅ Supported | LTS - Supported until November 2026 |
| .NET 10.0 | ✅ Supported | Current - Supported until November 2027 |

> ⚠️ **Note on .NET Framework**: Although `netstandard2.0` is technically compatible with .NET Framework 4.6.1+, the reliable minimum for a complete `netstandard2.0` implementation is **.NET Framework 4.7.2**. Using `net461` may result in runtime errors (`MissingMethodException`, `TypeLoadException`).

## Package Versions

The library uses a split strategy to ensure both compatibility and security depending on the target framework:

| Framework | ASP.NET Core packages | Strategy |
|-----------|----------------------|----------|
| `netstandard2.0` | `Microsoft.AspNetCore.Mvc.Core` 2.3.9<br>`Microsoft.AspNetCore.HttpOverrides` 2.3.9 | NuGet packages (series 2.3.x) |
| `net6.0` | *(shared framework)* | `FrameworkReference Microsoft.AspNetCore.App` |
| `net8.0` | *(shared framework)* | `FrameworkReference Microsoft.AspNetCore.App` |
| `net10.0` | *(shared framework)* | `FrameworkReference Microsoft.AspNetCore.App` |

### Why `FrameworkReference` for net6.0+?

From ASP.NET Core 3.0 onwards, Microsoft no longer distributes `Microsoft.AspNetCore.*` packages as standalone NuGet packages for modern frameworks. They are part of the **shared framework** (`Microsoft.AspNetCore.App`), which ships with the .NET SDK. Using `FrameworkReference` instead of `PackageReference` for these targets:

- ✅ Avoids referencing legacy/deprecated NuGet packages
- ✅ Eliminates dependency vulnerability overrides (`System.Text.Encodings.Web`, `Newtonsoft.Json`)
- ✅ Always resolves to the version installed on the target machine
- ✅ Reduces NuGet package size

### Why series `2.3.x` for `netstandard2.0`?

The `2.3.x` series is the active maintenance branch Microsoft created to keep `Microsoft.AspNetCore.*` packages working in `netstandard2.0` libraries. It supersedes the deprecated `2.1.x` / `2.2.x` series:

| Series | Status | Notes |
|--------|--------|-------|
| `2.1.x` | ⚠️ Deprecated | EOL, known vulnerabilities |
| `2.2.x` | ⚠️ Deprecated | EOL, known vulnerabilities |
| `2.3.x` | ✅ Active | Actively maintained, `netstandard2.0` compatible |

## Third-party Dependencies

| Package | Version | Targets | Notes |
|---------|---------|---------|-------|
| `IPNetwork2` | 3.1.764 | All | IP network/range calculation. Compatible with `netstandard2.0` and `net8.0` natively. |

## Security Considerations

### Vulnerability Mitigations

#### Previous vulnerabilities (resolved by upgrading to 2.3.x)

The original `netstandard2.0` dependencies (`Microsoft.AspNetCore.Mvc.Core 2.2.5`, `Microsoft.AspNetCore.HttpOverrides 2.1.1`) required explicit vulnerability overrides:

| Package | Override | CVE / Reason |
|---------|----------|--------------|
| `System.Text.Encodings.Web` | 4.5.1 | Transitive vulnerability via 2.2.x chain |
| `Newtonsoft.Json` | 13.0.3 | CVE-2024-21907 — DoS via improper exception handling |

After upgrading to `2.3.9`, these overrides are **no longer needed** as the updated dependency chain resolves them.

### Version Selection Strategy

The package versions were selected using the **Minimum Viable Product (MVP)** approach:
- ✅ Uses the **minimum version** required to address known vulnerabilities
- ✅ Avoids unnecessary updates that might introduce breaking changes
- ✅ Maintains compatibility with older frameworks via `netstandard2.0`
- ✅ Regular security audits using `dotnet list package --vulnerable`

## Package Version Analysis

### .NET Standard 2.0

| Package | Current | Latest for framework | What's missing |
|---------|---------|---------------------|----------------|
| `Microsoft.AspNetCore.Mvc.Core` | **2.3.9** | 2.3.9 | ✅ Using latest for `netstandard2.0` |
| `Microsoft.AspNetCore.HttpOverrides` | **2.3.9** | 2.3.9 | ✅ Using latest for `netstandard2.0` |
| `IPNetwork2` | **3.1.764** | 3.4.853 | New features (ParseRange, operators). No security issues in 3.1.764. |

**Notes**:
- `2.3.9` is the latest version of the `2.x` series supporting `netstandard2.0`
- Versions `3.0+` of `Microsoft.AspNetCore.*` target .NET Core 3.0+ exclusively
- No known security vulnerabilities in `2.3.9`

### .NET 6.0 (LTS — End of Life November 2024)

| Component | Strategy | Notes |
|-----------|----------|-------|
| ASP.NET Core | `FrameworkReference` (shared framework) | Resolves to the .NET 6.x SDK installed on the machine |
| `IPNetwork2` | **3.1.764** | Resolves via `netstandard2.0` target. Compatible. |

**Recommendation**: ⚠️ Framework is EOL since November 2024. Plan migration to .NET 8.0 LTS or .NET 10.0.

### .NET 8.0 (LTS — Supported until November 2026)

| Component | Strategy | Notes |
|-----------|----------|-------|
| ASP.NET Core | `FrameworkReference` (shared framework) | Resolves to the .NET 8.x SDK installed on the machine |
| `IPNetwork2` | **3.1.764** | Resolves via `net8.0` native target. Optimized binary. |

**Recommendation**: ✅ Active LTS. Recommended for production workloads.

### .NET 10.0 (Current — Supported until November 2027)

| Component | Strategy | Notes |
|-----------|----------|-------|
| ASP.NET Core | `FrameworkReference` (shared framework) | Resolves to the .NET 10.x SDK installed on the machine |
| `IPNetwork2` | **3.1.764** | Resolves via `net8.0` target (closest available). Compatible. |

**Recommendation**: ✅ Latest release. Recommended for new projects.

## Update Strategy Recommendations

### Priority 1: Critical (Do immediately)
- **None** — All frameworks are using versions without critical unpatched vulnerabilities.

### Priority 2: High (Within 1 month)
- **None** — No pending high-severity updates.

### Priority 3: Medium (When convenient)
- **`IPNetwork2`**: Consider upgrading to `3.4.853`. See details below.

### Priority 4: Low
- **.NET 6.0**: Framework is EOL. Plan migration to .NET 8.0 LTS or .NET 10.0.

## IPNetwork2 Upgrade Analysis (`3.1.764` → `3.4.853`)

No security vulnerabilities have been reported in any version of `IPNetwork2`. The following is a functional breakdown of what has been added since `3.1.764`, grouped by category.

### 🧮 Mathematical Operations *(added in 3.2)*

| Feature | Description |
|---------|-------------|
| `+` / `-` operators | Arithmetic operations on `IPNetwork2` instances for intuitive network calculations |
| Enhanced subtraction | More complete network subtraction for complex IP range manipulations |

### 🌐 Parsing & Input Handling *(added in 3.2)*

| Feature | Description |
|---------|-------------|
| Network-aware CIDR guessing | Intelligent CIDR block detection/suggestion based on network topology |
| `TryParse` with sanitization | Built-in input sanitization for more robust and error-resistant parsing |

### 🔃 Comparison & Sorting *(changed in 3.3 — ⚠️ Breaking)*

| Feature | Description |
|---------|-------------|
| Updated sort order | IPNetwork comparison and sorting behavior updated to match industry standards |
| Fixed enum definitions | Removed obsolete enum definitions; corrected comparison logic |
| `ListIPAddress` static method | New static method for listing IP addresses within a network |
| Improved `TryParse` | Additional `TryParse` overloads with better error handling |

> ⚠️ **Breaking change in 3.3**: If your code relies on `IPNetwork2` sorting or comparison operators, behavior may differ. Review before upgrading.

### 📐 Range Parsing *(added in 3.4)*

| Feature | Description |
|---------|-------------|
| `ParseRange(string)` | Converts an IP range (`"start - end"`) into the minimal set of CIDR blocks covering it |
| `TryParseRange(string)` | Non-throwing variant of `ParseRange` |
| IPv4 and IPv6 support | Both address families supported |
| Whitespace trimming | Input is automatically sanitized |

**Example** (3.4):
```csharp
// "192.168.1.45 - 192.168.1.65" → [192.168.1.45/32, 192.168.1.46/31, 192.168.1.48/28, 192.168.1.64/31]
var blocks = IPNetwork2.ParseRange("192.168.1.45 - 192.168.1.65");
```

### Summary

| Version | Type of change | Breaking? |
|---------|---------------|-----------|
| 3.2.x | New features (operators, CIDR guessing, better `TryParse`) | ✅ No |
| 3.3.x | Cleanup + new features + sorting fix | ⚠️ Yes — sort/comparison behavior |
| 3.4.x | New features (`ParseRange`, `TryParseRange`) | ✅ No |

**Recommendation**: Upgrading to `3.4.853` is safe if this project **does not rely on `IPNetwork2` comparison/sorting order**. Given that the library only uses `.Contains()` for IP membership checks, the breaking change in 3.3 is **not applicable here**.

---

## Migration Path

If you are consuming this library from a .NET Framework application:

| .NET Framework version | Compatibility |
|------------------------|---------------|
| 4.6.1 | ⚠️ Technically compatible, runtime issues possible |
| 4.7.2 | ✅ Minimum recommended |
| 4.8 | ✅ Recommended for .NET Framework |

### Upgrading the library target

If you want to drop `netstandard2.0` support in the future:

1. Remove the `netstandard2.0` `ItemGroup` with NuGet packages
2. Remove `netstandard2.0` from `<TargetFrameworks>`
3. All remaining targets use `FrameworkReference` — no additional changes needed

## Verification

To verify there are no known vulnerabilities in this package:

```powershell
dotnet list package --vulnerable --include-transitive
```

Expected output:
```
The given project has no vulnerable packages given the current sources.
```

## Additional Resources

- [Microsoft .NET Support Policy](https://dotnet.microsoft.com/platform/support/policy)
- [ASP.NET Core Security Advisories](https://github.com/dotnet/announcements/issues?q=is%3Aissue+label%3ASecurity)
- [NuGet Package Vulnerabilities](https://github.com/advisories?query=ecosystem%3Anuget)
- [.NET Standard compatibility table](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)

## Last Updated

Document last updated: February 25, 2026

For the latest information, please check the [GitHub repository](https://github.com/hfrances/qckdev.AspNetCore.Mvc.Filters.IpSafe).
