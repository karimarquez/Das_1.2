# Guía de despliegue

## 1. Alcance y requisitos de producción

La aplicación está dirigida a .NET 10 y actualmente persiste sus datos en SQLite. El despliegue debe conservar el archivo de la base de datos en almacenamiento persistente, permitir escritura solo a la identidad del proceso y tener copias de seguridad verificables.

Para una instalación de una sola instancia, SQLite puede utilizarse si el almacenamiento es persistente. No escales horizontalmente una instancia SQLite local. Para alta disponibilidad o varias instancias, primero migra y valida la aplicación contra una base de datos administrada.

## 2. Validar y publicar

En PowerShell, desde la raíz:

```powershell
.\scripts\publish.ps1
```

El script ejecuta las pruebas de la solución y publica solo si pasan. El resultado se genera en `src/DAS.Web/bin/Release/net10.0/publish`. Despliega el contenido de esa carpeta, no `bin/Debug` ni los archivos de desarrollo.

## 3. Configuración de producción

Configura los valores mediante variables de entorno, el almacén de secretos del proveedor o un gestor de secretos. No guardes valores reales en `appsettings*.json` versionados.

| Clave | Requisito |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena SQLite con ruta al archivo persistente y escribible |
| `Auth__BootstrapAdminEmail` | Correo del administrador inicial |
| `Auth__BootstrapAdminPassword` | Contraseña inicial fuerte; guárdala en un almacén seguro |
| `Jwt__SigningKey` | Secreto aleatorio de al menos 32 caracteres |
| `Jwt__Issuer` / `Jwt__Audience` | Opcional; valores estables compartidos por emisión y validación |
| `Swagger__Habilitado` | Déjalo deshabilitado públicamente salvo que se proteja y se requiera |

La aplicación aplica migraciones al arrancar. Haz una copia de seguridad antes del primer arranque de una versión que incluya migraciones.

## 4. Opción A: IIS

1. Instala el Hosting Bundle de ASP.NET Core correspondiente a .NET 10 en el servidor.
2. Copia el contenido publicado a una carpeta de versión, por ejemplo `C:\inetpub\DAS\releases\<version>`.
3. Crea un application pool dedicado con **No Managed Code** y configura el sitio para usar el directorio publicado.
4. Configura el entorno `Production` y los secretos en el entorno protegido del servidor.
5. Ubica el archivo SQLite fuera de la carpeta de publicación. Otorga permiso de modificación sobre la carpeta de datos únicamente a la identidad del application pool.
6. Configura el binding HTTPS y un certificado válido. No expongas directamente el puerto de desarrollo.
7. Habilita la recolección y rotación de logs del proceso; protege los archivos de log con permisos adecuados.
8. Reinicia el sitio y valida `https://<dominio>/health/live` y `https://<dominio>/health/ready`.
9. Inicia sesión con un usuario de prueba y valida inventario, movimiento, conteo, receta y reporte.
10. Conserva la versión previa y una copia de seguridad de SQLite para permitir recuperación.

## 5. Opción B: Azure App Service

1. Crea un App Service compatible con .NET 10 y configura el entorno `Production`.
2. Configura las claves del apartado anterior como Application Settings o referencias a Azure Key Vault; nunca las incluyas en el paquete.
3. Configura almacenamiento persistente para el archivo SQLite y usa una sola instancia mientras SQLite sea el proveedor. En Linux, valida la ruta de almacenamiento persistente habilitada para el App Service; en Windows, utiliza el almacenamiento persistente del sitio, no un directorio temporal.
4. Despliega únicamente los archivos de publicación mediante el método aprobado por el equipo.
5. Habilita HTTPS Only y configura certificados/dominio si aplica.
6. Habilita los logs de aplicación y define retención y acceso. Para monitoreo centralizado, integra Application Insights o el servicio de observabilidad aprobado.
7. Configura `/health/ready` como health check del App Service cuando la configuración del plan lo permita.
8. Valida las dos rutas de salud y completa el flujo funcional con una cuenta de prueba.
9. Comprueba persistencia de datos tras reiniciar la aplicación y verifica que existe una copia de seguridad recuperable.

## 6. Comprobación posterior

- Health checks responden `200` y solo exponen el estado, no detalles de base de datos ni secretos.
- HTTPS y autenticación están activos.
- Los permisos distinguen Consulta, Encargado y Admin.
- Crear un movimiento y un conteo conserva su historial después de reiniciar.
- Un error inesperado muestra una página segura y un código de solicitud; los detalles quedan solo en logs protegidos.
- CI y las pruebas funcionales pasan sobre el commit desplegado.

## 7. Recuperación

Si falla la validación, retira el tráfico o vuelve a la versión publicada anterior. Restaura una copia de seguridad solo después de confirmar el punto de recuperación y detener las escrituras. Registra el incidente y la versión afectada; no compartas los logs completos si contienen datos operativos.
