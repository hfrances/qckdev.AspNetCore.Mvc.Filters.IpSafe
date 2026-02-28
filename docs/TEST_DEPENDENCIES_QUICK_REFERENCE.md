# Referencia Rapida: Tests Multi-Framework

## Proyecto
`qckdev.AspNetCore.Mvc.Filters.IpSafe.Test`

## Checklist

1. No cambiar `TargetFrameworks` sin solicitud explicita.
2. Mantener bloque de testing sin `Condition`.
3. Mantener versiones unificadas de MSTest/Coverlet.
4. Validar `dotnet test` y `--list-tests`.
5. No introducir `Test.Common` (este repo no lo usa).

## Frameworks activos

`netcoreapp3.1;net5.0;net6.0;net8.0;net10.0`

## Paquetes de testing

- `Microsoft.NET.Test.Sdk` `17.11.1`
- `MSTest.TestAdapter` `3.2.2`
- `MSTest.TestFramework` `3.2.2`
- `coverlet.msbuild` `6.0.0`
- `coverlet.collector` `6.0.0`

