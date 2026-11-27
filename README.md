# DAS - Inventario de bar

Aplicación ASP.NET Core MVC con SQLite, Entity Framework Core, ASP.NET Core Identity y una API protegida con JWT, organizada en capas según la arquitectura limpia.

## Estructura de la solución

```text
DAS.sln
src/
  DAS.Domain/          Entidades y reglas del negocio. No depende de ningún otro proyecto ni framework.
  DAS.Application/     Servicios (casos de uso), DTOs e interfaces de repositorios y unidad de trabajo.
  DAS.Infrastructure/  EF Core: AppDbContext, configuraciones, repositorios, unidad de trabajo, migraciones,
                       datos iniciales, Identity y exportación CSV.
  DAS.Web/             Controladores MVC, vistas Razor, ViewModels, API, Swagger, seguridad y composición de dependencias.
tests/
  DAS.Tests/           Pruebas unitarias, de integración (SQLite en memoria), funcionales (HTTP) y de arquitectura.
docs/
  arquitectura.md      Guía de arquitectura: capas, repositorios, unidad de trabajo y dónde va cada cambio.
  experiencia-usuario.md  Necesidades, perfiles, historias con criterios de aceptación y flujos.
  seguridad-asincronia-y-api.md  Etapa 4: protección (CSRF, XSS, validaciones), AJAX/Fetch, Swagger y pruebas de la API.
  prototipo-inicial/   Prototipo estático anterior (barra superior y tarjetas), solo como antecedente.
```

Las dependencias van siempre hacia el dominio: `Web → Application → Domain` e `Infrastructure → Application → Domain`. La capa web solo usa Infrastructure para registrar dependencias e Identity; los controladores trabajan con interfaces de servicios. Las pruebas de `tests/DAS.Tests/Architecture` fallan si alguien rompe estas reglas.

## Ejecutar localmente

Requiere el SDK de .NET 10. Desde la carpeta raíz:

```powershell
dotnet run --project src/DAS.Web
```

La aplicación aplica las migraciones y carga datos de demostración en SQLite en el primer inicio (`src/DAS.Web/PipirisNais.db`). Con el perfil HTTP, abre `http://localhost:5257`.

> Si ya tenían una base `PipirisNais.db` en la raíz del proyecto anterior, cópienla a `src/DAS.Web/` para conservar sus datos.

## Pruebas

```powershell
dotnet test
```

| Carpeta | Qué comprueba |
|---|---|
| `Unit/Domain` | Reglas de la entidad producto: existencia nunca negativa, ajustes, conversión de unidades. |
| `Unit/Application` | Servicios con repositorios en memoria: validaciones, transacciones, usuario que registra. |
| `Unit/Infrastructure` | Exportación CSV. |
| `Integration` | Repositorios, unidad de trabajo, migraciones y restricciones con SQLite en memoria. |
| `Functional` | La aplicación completa por HTTP: autorización por rol, antifalsificación, recetas, reportes y API JWT. |
| `Functional/SecurityTests`, `ValidationTests` | Encabezados y CSP, CSRF en formularios y AJAX, XSS, validación de cliente y servidor, límite de intentos. |
| `Functional/AsyncEndpointsTests` | Endpoints JSON que consume el sitio con Fetch y jQuery AJAX. |
| `Functional/ProductApiTests`, `ApiDocumentationTests` | API JWT de punta a punta, contrato de respuestas, consistencia de datos y documento OpenAPI. |
| `Architecture` | Dependencias permitidas entre capas y contenedor de dependencias. |

## Migraciones (Entity Framework Core)

Las migraciones viven en `DAS.Infrastructure`; el proyecto de inicio es `DAS.Web`:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project src/DAS.Infrastructure --startup-project src/DAS.Web
```

No hace falta ejecutar `dotnet ef database update`: la aplicación aplica las migraciones pendientes al iniciar.

## Dónde agregar código nuevo

| Necesito... | Lugar |
|---|---|
| Una regla del negocio sobre una entidad | Método en la entidad de `DAS.Domain/Entities` |
| Una consulta LINQ nueva | Método en la interfaz de `DAS.Application/Abstractions/Persistence` y su implementación en `DAS.Infrastructure/Persistence/Repositories` |
| Un caso de uso | Servicio en `DAS.Application/Services` con su interfaz en `Services/Interfaces` |
| Una tabla o relación | Entidad en Domain, configuración en `Persistence/Configurations` y una migración |
| Un formato de exportación | Clase que implemente `IReportExporter` en `DAS.Infrastructure/Reports`, registrada en `DependencyInjection.cs` |
| Una pantalla | Controlador, ViewModel y vista en `DAS.Web` |

## Cuentas y roles

Registra una cuenta desde `/cuenta/registro`. Las cuentas nuevas tienen el rol `Consulta` y pueden ver el inventario, pero no realizar cambios.

Para crear un administrador local, configura sus credenciales en User Secrets del proyecto web (elige tu propia contraseña segura):

```powershell
dotnet user-secrets set "Auth:BootstrapAdminEmail" "admin@local.test" --project src/DAS.Web
dotnet user-secrets set "Auth:BootstrapAdminPassword" "<tu-contraseña-segura>" --project src/DAS.Web
```

Al iniciar la aplicación, ese usuario recibe el rol `Admin`. Desde el menú **Usuarios**, un administrador puede asignar los roles `Admin`, `Encargado` o `Consulta`. Los formularios de escritura requieren rol `Admin` o `Encargado`. Cada movimiento y conteo queda registrado con la cuenta que inició sesión.

## API JWT

`POST /api/auth/token` recibe JSON con `email` y `password` y devuelve un token Bearer. Úsar en `GET /api/inventario/productos`, que acepta `search`, `page` y `pageSize`:

```http
Authorization: Bearer <access_token>
```

En Desarrollo, si no se configura una clave JWT, se genera una clave temporal al iniciar la aplicación; por eso los tokens dejan de ser válidos al reiniciarla. En otros entornos configura una clave estable de al menos 32 caracteres como secreto y no la guardes en el repositorio:

```powershell
dotnet user-secrets set "Jwt:SigningKey" "<secreto-aleatorio-de-al-menos-32-caracteres>" --project src/DAS.Web
```

## Documentación de la API (Swagger)

Con la aplicación en Desarrollo abre `http://localhost:5257/swagger`. Ahí están todos los endpoints `/api/...` con sus esquemas, códigos de respuesta y ejemplos de error.

1. Ejecuta `POST /api/auth/token` desde Swagger.
2. Copia el `access_token` en el botón **Authorize**.
3. Prueba la API de productos.

Los endpoints de **sesión web** son los que usa el propio sitio con Fetch y jQuery AJAX: usan la cookie de la sesión y, si modifican datos, el encabezado `X-CSRF-TOKEN`. Para publicar Swagger en otro entorno configura `Swagger:Habilitado=true`.

El archivo `src/DAS.Web/DAS.Web.http` encadena las llamadas de la API JWT para probarlas desde Visual Studio. Coloca tu contraseña local en `src/DAS.Web/http-client.env.json.user`; Git no sube ese archivo.

## Seguridad y comunicación asíncrona

| Tema | Qué hace la aplicación |
|---|---|
| CSRF | Toda acción POST de las vistas exige el token antifalsificación; las llamadas AJAX lo envían en `X-CSRF-TOKEN` |
| XSS | Envía una política de contenido (CSP) estricta y encabezados de seguridad. Los campos de texto no aceptan `<` ni `>`, y la salida se codifica en HTML y en JSON |
| Validación | DataAnnotations en ViewModels y en `SaveProductDto`, validadas en el servidor y en el navegador con los mismos mensajes |
| Credenciales | Se permiten 10 intentos por minuto en inicio de sesión, registro y token (`Seguridad:IntentosDeCredencialesPorMinuto`); el bloqueo de Identity se mantiene |
| Interfaz | El tablero registra movimientos con Fetch, conteos con jQuery AJAX y se actualiza sin recargar; las recetas se preparan con Fetch |

El detalle de cada punto, con los archivos y pruebas que lo cubren, está en [docs/seguridad-asincronia-y-api.md](docs/seguridad-asincronia-y-api.md).

## Recuperación de contraseña

No está habilitada todavía: requiere configurar un proveedor de correo para enviar de forma segura los enlaces de restablecimiento de Identity.
