# Manual del desarrollador

## 1. Requisitos y ejecución local

- SDK de .NET 10.
- Git.
- Windows PowerShell para los comandos de este manual.

Desde la raíz del repositorio:

```powershell
dotnet restore DAS.sln
dotnet test DAS.sln
dotnet run --project src/DAS.Web/DAS.Web.csproj
```

La aplicación aplica migraciones y carga los datos iniciales al iniciar. La configuración de desarrollo se encuentra en `src/DAS.Web/appsettings.Development.json`; secretos locales deben guardarse con User Secrets, no en archivos versionados:

```powershell
dotnet user-secrets set "Auth:BootstrapAdminEmail" "admin@local.test" --project src/DAS.Web
dotnet user-secrets set "Auth:BootstrapAdminPassword" "<contraseña-local-segura>" --project src/DAS.Web
dotnet user-secrets set "Jwt:SigningKey" "<secreto-aleatorio-de-al-menos-32-caracteres>" --project src/DAS.Web
```

## 2. Arquitectura y ubicación de cambios

La solución se organiza en capas:

- `DAS.Domain`: entidades y reglas del negocio.
- `DAS.Application`: casos de uso, DTOs, validaciones e interfaces.
- `DAS.Infrastructure`: EF Core, SQLite, Identity, repositorios y migraciones.
- `DAS.Web`: controladores, vistas, API, seguridad, configuración y composición.
- `tests/DAS.Tests`: pruebas unitarias, funcionales y de arquitectura.

Las dependencias deben apuntar hacia el dominio. Mantén las reglas del negocio en Domain/Application y deja que los controladores de Web coordinen la entrada y la respuesta.

## 3. Base de datos

Las migraciones pertenecen a Infrastructure. Para crear una:

```powershell
dotnet ef migrations add NombreDescriptivo --project src/DAS.Infrastructure --startup-project src/DAS.Web
```

La aplicación aplica migraciones pendientes al iniciar. Haz una copia de seguridad antes de actualizar una base de producción. La implementación actual usa SQLite; para varias instancias o alta concurrencia, planifica una migración explícita a un motor administrado y prueba el proveedor antes del despliegue.

## 4. Logs, errores y monitoreo

Los servicios escriben eventos de negocio con `ILogger<T>`. En producción la aplicación emite logs JSON por consola; el host debe recolectar y conservar esa salida. No registres contraseñas, tokens, claves, cookies ni datos personales innecesarios.

- `GET /health/live`: comprueba que el proceso responde.
- `GET /health/ready`: comprueba que la aplicación puede conectarse a la base de datos.
- La excepción no controlada se maneja mediante el error MVC de producción; el usuario ve un código de solicitud y no la traza.
- Los errores API se expresan con Problem Details y códigos HTTP; no se deben devolver detalles internos de excepciones.

Incluye pruebas funcionales para cambios de respuestas, autorización y health checks. No captures excepciones de forma general para devolver un resultado exitoso o vacío.

## 5. Pruebas y publicación

Ejecuta todas las pruebas antes de integrar cambios:

```powershell
dotnet test DAS.sln --configuration Release
```

El script `scripts/publish.ps1` ejecuta las pruebas y solo si pasan publica `src/DAS.Web/DAS.Web.csproj` en configuración Release. Consulta [la guía de despliegue](./despliegue.md) para IIS y Azure.

## 6. Flujo de control de versiones

1. Actualiza `main` y crea una rama breve por issue o tarea: `feature/<tema>` o `fix/<tema>`.
2. Haz cambios enfocados y commits pequeños con mensajes descriptivos.
3. Ejecuta las pruebas localmente y revisa `git diff` antes de enviar.
4. Abre un pull request hacia `main`, describe el cambio y enlaza su issue.
5. Espera que CI termine correctamente y solicita revisión de otra persona.
6. Resuelve conflictos integrando ambas intenciones; no elimines cambios locales para forzar un merge.
7. Integra el PR mediante el mecanismo acordado por el equipo y confirma que `main` sigue verde.

La acción de GitHub Actions en `.github/workflows/ci.yml` ejecuta `dotnet test` en pushes y pull requests a `main`. Activa protección de rama en GitHub para requerir esa comprobación y revisión antes de integrar.
