# GlowUp API

La API de GlowUp usa .NET 8 y PostgreSQL en Neon. Cada desarrollador ejecuta
la API en su computadora, pero todos los entornos de desarrollo autorizados se
conectan a la base compartida de Neon.

## Configuración local

Necesitas .NET 8 y Node.js. Pide al responsable del proyecto el connection
string de Neon **por un canal privado**. No lo copies a Git, `appsettings.json`
ni archivos `.env` versionados.

Desde la raíz del repositorio, guarda los secretos locales:

```powershell
dotnet user-secrets set --project ".\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj" "ConnectionStrings:DefaultConnection" "Host=TU_HOST;Port=5432;Database=TU_BASE;Username=TU_USUARIO;Password=TU_PASSWORD;Ssl Mode=Require;"

dotnet user-secrets set --project ".\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj" "Jwt:Key" "UNA_CLAVE_LARGA_Y_UNICA"
```

El identificador de User Secrets pertenece al proyecto, no a su carpeta. Por
eso los secretos continúan funcionando después de la reestructuración mientras
se conserve el `UserSecretsId` del archivo de proyecto.

## Ejecutar la aplicación

```powershell
dotnet restore ".\GlowUp.API\GlowUp.API.sln"
dotnet tool restore --tool-manifest ".\GlowUp.API\dotnet-tools.json"
dotnet run --project ".\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj" --launch-profile http
```

La API queda disponible en `http://localhost:5297` y Swagger en
`http://localhost:5297/swagger`.

En otra terminal configura y ejecuta el frontend:

```text
# GlowUp.Web/.env
VITE_AUTH_MODE=api
VITE_API_URL=
VITE_API_PROXY_TARGET=http://localhost:5297
```

```powershell
cd .\GlowUp.Web
npm install
npm run dev
```

Abre `http://localhost:5173`.

## Datos demo

Al registrar un negocio, la API crea automáticamente datos demo aislados para
ese negocio: catálogo, personal, clientes y citas. Para completar negocios
vacíos creados antes de esta función, ejecuta:

```powershell
dotnet run --project ".\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj" -- --seed-demo-data
```

El comando es idempotente: no duplica datos y no altera negocios que ya tengan
categorías, servicios, empleados, clientes o citas. Como Neon es compartido,
úsalo únicamente cuando el equipo esté de acuerdo.

Consulta [la guía de base de datos](Database/README.md) antes de modificar el
esquema o regenerar las entidades.
