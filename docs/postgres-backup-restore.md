# Respaldo y restauración de PostgreSQL en Render

Este documento es un procedimiento: **no se descargó ni se modificó ninguna base de datos**. La base contiene datos de clientes, pedidos, usuarios y hashes de contraseñas. Guardá los archivos fuera del repositorio, en un lugar cifrado y con acceso restringido.

## Antes de comenzar

1. En Render, abrí la base `nexo-postgres` y comprobá que esté activa. En el plan gratuito, la base tiene plazo de vencimiento y Render no ofrece backups administrados; verificá la fecha y el plan actuales.
2. Instalá PostgreSQL Client Tools en una computadora segura. Usá una versión de `pg_dump` compatible con la versión del servidor (el Blueprint declara PostgreSQL 17).
3. En Render → base de datos → **Connections**, anotá el host externo, puerto, nombre de base y usuario. No copies la contraseña a un chat, archivo del repositorio ni línea de comandos. Si Render requiere una lista de IP permitidas, autorizá temporalmente tu IP para la operación.

## Crear un respaldo

En PowerShell, fuera del repositorio, reemplazá los marcadores por los datos de Render. `-W` pide la contraseña de forma interactiva.

```powershell
$backupFile = Join-Path $env:USERPROFILE ("nexo-{0:yyyyMMdd-HHmm}.dump" -f (Get-Date))
pg_dump -h <HOST_EXTERNO> -p <PUERTO> -U <USUARIO> -d <BASE_DE_DATOS> -W -Fc --no-owner --no-acl -f $backupFile
if ($LASTEXITCODE -ne 0) { throw "Falló pg_dump" }
pg_restore --list $backupFile | Select-Object -First 20
if ($LASTEXITCODE -ne 0) { throw "El archivo no pudo leerse" }
```

Mover luego el archivo a almacenamiento cifrado y registrar su fecha, tamaño y ubicación segura. No dejar copias en Descargas ni subirlo a GitHub. La lista de `pg_restore` verifica que el archivo se puede leer; **no demuestra que la restauración funciona**.

## Probar la restauración

Crear una base **de prueba vacía y separada** con permisos adecuados. No ejecutar estos pasos contra producción ni reutilizar la conexión de la aplicación. Después, restaurar allí:

```powershell
pg_restore -h <HOST_DE_PRUEBA> -p <PUERTO> -U <USUARIO_DE_PRUEBA> -d <BASE_DE_PRUEBA_VACIA> -W --no-owner --no-acl --exit-on-error $backupFile
if ($LASTEXITCODE -ne 0) { throw "Falló la restauración de prueba" }
```

Comprobar en esa base el recuento de clientes, pedidos, oportunidades, actividades y usuarios; probar el inicio de sesión de una cuenta de prueba si el entorno está aislado. Registrar resultado y fecha. Eliminar la base de prueba según la política de retención, con autorización de quien administra los datos.

## Frecuencia

Hacer un respaldo antes de migraciones y cambios de versión, y luego con la frecuencia que el negocio acepte como pérdida máxima de datos. Repetir la prueba de restauración periódicamente. Si no hay una copia restaurable reciente, **no asumir que los datos están protegidos**.

Referencias: [Render Free](https://render.com/docs/free), [Render PostgreSQL backups](https://render.com/docs/postgresql-backups), [pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html), [pg_restore](https://www.postgresql.org/docs/current/app-pgrestore.html).
