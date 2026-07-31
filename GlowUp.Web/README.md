# GlowUp Web

Frontend de GlowUp construido con React y Vite.

## Demo solo frontend

El modo predeterminado es `local`: registro, inicio de sesion y sesion se
guardan en el navegador sin depender de la API. Este modo es apropiado
unicamente para demostraciones.

```bash
npm install
npm run dev
```

Cada navegador mantiene sus propios usuarios en `localStorage`. Limpiar los
datos del sitio elimina las cuentas de demostracion.

## Desarrollo con API

El repo incluye una configuracion local ignorada por git en `.env` para usar la
API de desarrollo con el perfil `http`:

```bash
VITE_AUTH_MODE=api
VITE_API_URL=
VITE_API_PROXY_TARGET=http://localhost:5297
```

Inicia la API desde la raíz del repositorio y luego ejecuta el frontend:

```bash
cd ..
dotnet restore .\GlowUp.API\GlowUp.API.sln
dotnet run --project .\GlowUp.API\GlowUp.Api\GlowUpRD.API.csproj --launch-profile http

cd .\GlowUp.Web
npm install
npm run dev
```

Vite publica la aplicacion en `http://localhost:5173` y redirige `/api` hacia
la API local. Puedes cambiar el destino con `VITE_API_PROXY_TARGET`. La guía
completa para conectar Neon y configurar los User Secrets está en
[`GlowUp.API/README.md`](../GlowUp.API/README.md).
