# PostgreSQL Database First

## Create the database and apply the schema

Install PostgreSQL locally (any recent version), then create the database and run the
schema script from `Database/Scripts/schema_postgresql.sql`:

```powershell
createdb -U postgres glowuprd_db
psql -U postgres -d glowuprd_db -f Database\Scripts\schema_postgresql.sql
```

The script creates all tables (named in Spanish, matching the API's DTOs) with their
indexes, foreign keys, and the `set_actualizado_en()` trigger that replaces MySQL's
`ON UPDATE CURRENT_TIMESTAMP` behavior.

## Configure the connection

For local development, store the complete connection string with .NET user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=glowuprd_db;Username=postgres;Password=your_password;"
```

Do not commit real database credentials.

## Restore the EF CLI

```powershell
dotnet tool restore
```

The solution includes a local `dotnet-ef` 8.0.13 tool manifest, so a global
installation is not required.

## Generate the context and entities

Run this command from the solution directory (`GloupUpRD.API`):

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL --project GlowUp.Core\GlowUp.Core.csproj --startup-project GloupUpRD.API\GloupUpRD.API.csproj --context GlowUpDbContext --context-dir Data --output-dir Models --namespace GloupUpRD.API.Models --context-namespace GloupUpRD.API.Data --no-onconfiguring --force -- --environment Development
```

The command reads the existing PostgreSQL schema and regenerates
`GlowUp.Core/Data/GlowUpDbContext.cs` and the entity classes under
`GlowUp.Core/Models` (in Spanish: `Cita`, `Negocio`, `Sucursal`, etc.). Re-run it after
database schema changes (update `Database/Scripts/schema_postgresql.sql` first, apply
it, then re-scaffold), and adapt application services if the database contract changed.
