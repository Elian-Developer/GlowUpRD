# PostgreSQL Database First (Neon)

El proyecto usa una única base de datos PostgreSQL compartida, alojada en
[Neon](https://neon.tech). No hay ninguna base de datos local que instalar —
todos (y todas las máquinas) se conectan a la misma base en la nube, así que el
esquema y los datos se mantienen sincronizados automáticamente entre todo el equipo.

## Sumarse a un proyecto ya existente (el caso más común)

Si el proyecto de Neon ya existe, solo necesitas el connection string:

1. Pídele a un compañero el connection string de Neon (compártanlo de forma privada —
   gestor de contraseñas, mensaje directo, etc. — **nunca** por GitHub, issues, ni
   ningún historial de chat que no sea privado).
2. Guárdalo con los user secrets de .NET. El connection string de Neon tiene esta
   forma: `postgresql://usuario:password@host/db?sslmode=require`; conviértelo al
   formato de pares separados por `;` que espera Npgsql:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host-de-neon>;Port=5432;Database=<db-de-neon>;Username=<usuario-de-neon>;Password=<password-de-neon>;Ssl Mode=Require;" --project "GloupUpRD.API\GloupUpRD.API.csproj"
   ```

   Neon siempre usa el puerto 5432 y exige SSL, así que `Port=5432;Ssl Mode=Require;`
   nunca cambian entre compañeros de equipo — solo `Host`, `Database`, `Username` y
   `Password` vienen del connection string que te dio Neon.
3. Corre `dotnet tool restore` una vez (restaura la herramienta local `dotnet-ef` desde
   el manifiesto, no hace falta instalación global).
4. Corre la API. Si conecta, ya está — el esquema y los datos existentes ya están ahí.

No commitees credenciales reales de la base de datos. `appsettings.json`/
`appsettings.Development.json` solo contienen valores de relleno; el valor real vive
exclusivamente en los user secrets, que se guardan fuera del repositorio en cada
máquina.

## Configurar un proyecto de Neon nuevo desde cero (solo se hace una vez, o para un entorno nuevo)

1. Crea un proyecto en [console.neon.tech](https://console.neon.tech).
2. Abre el **SQL Editor** del proyecto y corre el contenido completo de
   `Database/Scripts/schema_postgresql.sql`. Esto crea las 23 tablas (nombradas en
   español, igual que los DTOs de la API) con sus índices, llaves foráneas, y el
   trigger `set_actualizado_en()` que mantiene actualizadas las columnas
   `actualizado_en` en cada modificación.
3. Copia el connection string desde el panel de Neon y sigue los pasos de "Sumarse a
   un proyecto ya existente" de arriba para configurarlo localmente.

## Regenerar el contexto y las entidades después de un cambio de esquema

Si modificas `Database/Scripts/schema_postgresql.sql` (agregas una columna, una tabla,
etc.), aplícalo primero en el SQL Editor de Neon, y luego vuelve a correr el scaffolding
desde la carpeta de la solución (`GloupUpRD.API`):

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL --project GlowUp.Core\GlowUp.Core.csproj --startup-project GloupUpRD.API\GloupUpRD.API.csproj --context GlowUpDbContext --context-dir Data --output-dir Models --namespace GloupUpRD.API.Models --context-namespace GloupUpRD.API.Data --no-onconfiguring --force -- --environment Development
```

Esto lee el esquema actual desde Neon y regenera `GlowUp.Core/Data/GlowUpDbContext.cs`
y las clases de entidad bajo `GlowUp.Core/Models` (en español: `Cita`, `Negocio`,
`Sucursal`, etc.). Ajusta los servicios de la aplicación después si el contrato de la
base de datos cambió.

## Configuración del frontend

El connection string de la API de arriba solo cubre el backend. Para correr la app web
contra esa base, sigue la sección "Desarrollo con API" de `GlowUp.Web/README.md` (copia
`.env.example` a `.env` y define `VITE_AUTH_MODE=api`).
