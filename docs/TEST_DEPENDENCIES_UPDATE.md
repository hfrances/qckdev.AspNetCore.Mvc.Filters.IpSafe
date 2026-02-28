# Actualizacion de Dependencias de Tests Unitarios

## Proyecto
`qckdev.AspNetCore.Mvc.Filters.IpSafe.Test`

## Estado actual (fuente de verdad)

- Target frameworks: `netcoreapp3.1;net5.0;net6.0;net8.0;net10.0`
- Test SDK/MSTest/Coverlet: mismas versiones para todos los frameworks
- No existe proyecto `Test.Common` en este repositorio

## Regla de implementacion para este proyecto

1. Mantener `TargetFrameworks` exactamente como estan, salvo solicitud explicita.
2. Mantener un unico `ItemGroup` sin `Condition` para paquetes de testing.
3. No crear bloque legacy (`net461`) porque no existe ese target en este proyecto.
4. Verificar discovery de tests tras cualquier cambio de versiones.

## Matriz de versiones

### Testing (todos los frameworks)

- `Microsoft.NET.Test.Sdk`: `17.11.1`
- `MSTest.TestAdapter`: `3.2.2`
- `MSTest.TestFramework`: `3.2.2`
- `coverlet.msbuild`: `6.0.0`
- `coverlet.collector`: `6.0.0`

## Verificacion

```powershell
dotnet test qckdev.AspNetCore.Mvc.Filters.IpSafe.Test\qckdev.AspNetCore.Mvc.Filters.IpSafe.Test.csproj
dotnet test qckdev.AspNetCore.Mvc.Filters.IpSafe.Test\qckdev.AspNetCore.Mvc.Filters.IpSafe.Test.csproj --list-tests
```

