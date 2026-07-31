# Base de datos PostgreSQL en Neon

## Conectarse a la base compartida

La base de desarrollo vive en Neon. No instales ni crees una base local para
trabajar con el proyecto compartido. Solicita al responsable los valores de
`Host`, `Database`, `Username` y `Password`, y guárdalos en User Secrets:

```powershell
dotnet user-secrets set --project ".\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj" "ConnectionStrings:DefaultConnection" "Host=TU_HOST;Port=5432;Database=TU_BASE;Username=TU_USUARIO;Password=TU_PASSWORD;Ssl Mode=Require;"
```

Neon requiere `Port=5432` y `Ssl Mode=Require`. No subas ese valor a Git ni lo
publiques en issues, chats públicos o documentación.

El esquema ya existe en la base compartida. Los colaboradores normales no deben
volver a ejecutar el script ni crear proyectos Neon separados.

## Crear un entorno Neon nuevo

Solo el responsable de un entorno nuevo debe:

1. Crear el proyecto y la base en Neon.
2. Abrir el SQL Editor.
3. Ejecutar por completo [Scripts/schema_postgresql.sql](Scripts/schema_postgresql.sql).
4. Configurar el connection string mediante User Secrets como se indica arriba.

El script crea las tablas, índices, claves foráneas y triggers que usa la API.

## Cambiar el esquema y regenerar entidades

Primero acuerda el cambio con el equipo. Después actualiza el script, aplícalo
en el entorno Neon correspondiente y regenera el contexto desde la raíz del
repositorio:

```powershell
dotnet tool restore --tool-manifest ".\GlowUp.API\dotnet-tools.json"

dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL --project ".\GlowUp.API\GlowUp.Core\GlowUp.Core.csproj" --startup-project ".\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj" --context GlowUpDbContext --context-dir Data --output-dir Models --namespace GlowUpRD.API.Models --context-namespace GlowUpRD.API.Data --no-onconfiguring --force -- --environment Development
```

Revisa y adapta los servicios, DTOs y pruebas después del scaffolding. Este
comando reemplaza las entidades y el contexto generados, por lo que no deben
guardarse reglas de negocio manuales dentro de esos archivos.
