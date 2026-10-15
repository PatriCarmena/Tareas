# Nexo · Gestión de clientes y pedidos

Aplicación MVC en ASP.NET Core 10 para administrar clientes y sus pedidos, persistida en PostgreSQL. Incluye una API externa mock independiente y un webhook idempotente para actualizar el estado de pedidos.

## Inicio rápido con Docker

Requiere Docker Desktop:

```powershell
docker compose up --build
```

Luego abrir `http://localhost:5080`. La base de datos se migra y carga con datos de demostración automáticamente. La API externa queda disponible en `http://localhost:5081`.

Usuario administrador local: `admin@nexo.local` / `NexoAdmin!2026`. Esta contraseña solo se usa en Development; en Render se genera una distinta.

Para detener los servicios sin borrar los datos:

```powershell
docker compose down
```

## Desarrollo local

Requiere .NET SDK 10 y PostgreSQL. Ejecutar ambos proyectos en terminales separadas:

```powershell
dotnet run --project src/OrderDesk.ExternalApi --urls http://localhost:5081
dotnet run --project src/OrderDesk.Web --urls http://localhost:5080
```

La conexión y las URL se configuran en `src/OrderDesk.Web/appsettings.json` o mediante variables de entorno.

## Webhook de pedidos

Endpoint: `POST /api/webhooks/orders`

```powershell
$body = '{"eventId":"evt-001","orderId":1,"status":"Delivered"}'
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5080/api/webhooks/orders `
  -Headers @{ "X-Webhook-Secret" = "development-secret" } `
  -ContentType "application/json" `
  -Body $body
```

Estados admitidos: `Pending`, `Confirmed`, `InProgress`, `Shipped`, `Delivered` y `Cancelled`. `eventId` es único; repetir el mismo evento no vuelve a aplicar la actualización.

En producción, definir secretos y credenciales fuera del repositorio, habilitar HTTPS y sustituir el encabezado compartido por una firma HMAC si el proveedor del webhook la soporta.

## Publicación en Render

El archivo `render.yaml` describe la aplicación, la API externa y PostgreSQL. Al crear un Blueprint en Render desde este repositorio se aprovisionan los tres recursos, se ejecutan las migraciones al iniciar y se generan automáticamente `Webhook__Secret` y `SeedAdmin__Password`.

Endpoints públicos esperados:

- Aplicación: `https://nexo-clientes-pedidos.onrender.com`
- Salud del backend: `GET /health`
- Webhook: `POST /api/webhooks/orders`
- API externa: `https://nexo-external-api.onrender.com/api/customer-insights/{customerId}`

Los valores reales se consultan en **Render → nexo-clientes-pedidos → Environment**. No deben publicarse en el repositorio:

- `SeedAdmin__Email` y `SeedAdmin__Password`: primer acceso administrativo.
- `Webhook__Secret`: autenticación del webhook.
- `Google__ClientId` y `Google__ClientSecret`: OAuth de Google. En Google Cloud registrar `https://nexo-clientes-pedidos.onrender.com/signin-google` como URI de redirección.
- `GmailApi__ClientId`, `GmailApi__ClientSecret` y `GmailApi__RefreshToken`: autorización OAuth 2.0 con alcance `gmail.send` para enviar códigos de recuperación mediante Gmail API.
- `GmailApi__FromAddress`: cuenta de Gmail que concedió la autorización. `GmailApi__FromName` puede configurarse como `Nexo`.

El plan gratuito de Render suspende los servicios inactivos, por lo que el primer acceso puede demorar. Evitar ese arranque en frío requiere cambiar el servicio web y, si corresponde, la API externa a un plan de pago.
