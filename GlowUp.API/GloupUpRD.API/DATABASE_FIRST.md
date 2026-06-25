# MySQL Database First

## Configure the connection

For local development, store the complete connection string with .NET user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=glowuprd_db;User=root;Password=your_password;"
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
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Pomelo.EntityFrameworkCore.MySql --project GlowUp.Core\GlowUp.Core.csproj --startup-project GloupUpRD.API\GloupUpRD.API.csproj --context GlowUpDbContext --context-dir Data --output-dir Models --namespace GloupUpRD.API.Models --context-namespace GloupUpRD.API.Data --no-onconfiguring --force -- --environment Development
```

The command reads the existing MySQL schema and regenerates
`GlowUp.Core/Data/GlowUpDbContext.cs` and the entity classes under
`GlowUp.Core/Models`. Re-run it after database schema changes, then compile and
adapt application services if the database contract changed.
