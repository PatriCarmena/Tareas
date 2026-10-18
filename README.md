# Nexo · Gestión de clientes y pedidos

Aplicación MVC en ASP.NET Core 10 para administrar clientes y sus pedidos, persistida en PostgreSQL. Incluye una API externa mock independiente y un webhook idempotente para actualizar el estado de pedidos.

## Actividades y seguimiento

Desde la ficha de un cliente, un administrador u operador puede programar llamadas, correos, reuniones o tareas. Cada actividad tiene fecha y hora de Buenos Aires, responsable, prioridad y descripción opcional. Se puede editar mientras esté pendiente y marcarla como realizada desde la ficha o desde **Actividades**. Los usuarios de consulta pueden verlas sin modificarlas.

La vista **Actividades** permite buscar por título o cliente y filtrar por tipo, estado o responsable actual. Una actividad pendiente pasa a mostrarse como **Vencida** automáticamente cuando supera su hora programada. El dashboard muestra las actividades programadas para hoy, las vencidas y **Mis tareas de hoy**. La ficha del cliente incorpora las actividades a la línea de tiempo; al completar una llamada, correo o reunión actualiza la fecha del último contacto.

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

## CRM: oportunidades comerciales

Desde la ficha de un cliente, un administrador u operador puede crear una oportunidad con nombre, importe estimado, probabilidad, fecha de cierre, responsable y etapa. El menú **Oportunidades** muestra un tablero Kanban con las etapas Prospecto, Contactado, Propuesta, Negociación, Ganada y Perdida. Se puede buscar por oportunidad o cliente, filtrar por responsable y cambiar la etapa arrastrando una tarjeta o usando su selector **Mover**. Los usuarios de consulta pueden ver el tablero, pero no modificarlo. Una oportunidad ganada puede convertirse en pedido desde el tablero mediante una confirmación explícita.

## Automatización (etapa 4)

- El dashboard destaca las actividades vencidas del usuario y sus próximos seguimientos de los siguientes siete días. Los avisos se calculan al abrir Nexo; el plan gratuito de Render suspende los procesos inactivos, así que no hay un programador de avisos en segundo plano.
- Al crear o mover una oportunidad a **Propuesta** o **Negociación**, Nexo crea una tarea de seguimiento para el responsable, con vencimiento a los dos días. Evita crear otra tarea idéntica mientras exista una pendiente.
- Una oportunidad **Ganada** muestra **Convertir en pedido**. La acción crea un pedido pendiente con un número derivado de la oportunidad, registra el origen y crea una tarea posventa. Un índice único evita más de un pedido por oportunidad. Después de convertirla no puede moverse a otra etapa ni cambiar su importe.
- Si Gmail API está configurada, la conversión envía un correo transaccional al responsable. Una falla de Gmail no deshace el pedido; se muestra un aviso. El envío de códigos de recuperación continúa usando la misma integración.
- `POST /api/webhooks/opportunities` recibe cambios externos de etapa con `eventId`, `opportunityId` y `stage`. Usa el mismo encabezado secreto `X-Webhook-Secret` que el webhook de pedidos, registra los eventos para evitar duplicados y crea tareas de seguimiento cuando corresponda. Las etapas admitidas son `Prospect`, `Contacted`, `Proposal`, `Negotiation`, `Won` y `Lost`. Al informar `Lost` también es obligatorio `lostReason` (máximo 250 caracteres). El webhook **no convierte** automáticamente una oportunidad ganada en pedido; la conversión requiere confirmar la acción dentro de Nexo.

## Informes y métricas (etapa 5)

El administrador puede abrir **Informes** y filtrar por fecha de creación y vendedor/responsable. Se muestran pedidos no cancelados por día y vendedor, oportunidades por etapa, conversión de oportunidades cerradas (ganadas / ganadas + perdidas), tiempo medio entre creación y cierre de oportunidades ganadas, actividades pendientes/vencidas, clientes sin contacto durante 30 días y motivos de pérdida. Cada conjunto puede descargarse como CSV UTF-8 con separador `;`, compatible con Excel y protegido contra fórmulas introducidas en campos de texto.

Los informes de oportunidades usan la cohorte creada en el período seleccionado y su etapa actual. Actividades y clientes sin contacto reflejan el estado actual, sin filtro de período. Los pedidos anteriores a esta etapa reciben un vendedor a partir del responsable actual de su oportunidad o cliente cuando es posible; para pedidos nuevos se guarda el vendedor en el momento de la creación. Las oportunidades ya cerradas antes de esta etapa no tienen fecha de cierre histórica confiable y quedan fuera del promedio de tiempo; los motivos de pérdida ausentes se muestran como «Sin registrar».

Ejemplo de webhook de oportunidad (con un ID existente y el secreto configurado, sin incluirlo en el repositorio):

```powershell
$body = '{"eventId":"opp-event-001","opportunityId":1,"stage":"Proposal"}'
Invoke-RestMethod -Method Post -Uri https://nexo-clientes-pedidos.onrender.com/api/webhooks/opportunities `
  -Headers @{ "X-Webhook-Secret" = $env:NEXO_WEBHOOK_SECRET } `
  -ContentType "application/json" -Body $body
```

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
