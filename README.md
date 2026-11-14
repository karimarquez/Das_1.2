# DAS - Inventario de bar

Aplicación ASP.NET Core MVC con SQLite, Entity Framework Core, ASP.NET Core Identity y una API protegida con JWT.

## Ejecutar localmente

Requiere el SDK de .NET 10.

```powershell
dotnet ef database update
dotnet run
```

La aplicación aplica las migraciones y carga datos de demostración en SQLite en el primer inicio. Con el perfil HTTP, abre `http://localhost:5257`.

## Cuentas y roles

Registra una cuenta desde `/cuenta/registro`. Las cuentas nuevas tienen el rol `Consulta` y pueden ver el inventario, pero no realizar cambios.

Para que una cuenta pueda iniciar sesión en cada clon con la misma credencial, configura el mismo correo y contraseña en User Secrets de cada equipo. User Secrets es local y no se sube a Git; la aplicación crea esa cuenta con rol `Admin` cuando todavía no existe en la base SQLite local:

```powershell
dotnet user-secrets set "Auth:BootstrapAdminEmail" "tu-correo@ejemplo.com"
dotnet user-secrets set "Auth:BootstrapAdminPassword" "<la-misma-contraseña-segura>"
```

Cada integrante debe ejecutar esos comandos en su clon y usar la misma contraseña acordada por un canal privado, nunca guardarla en el repositorio. Al iniciar, si la cuenta ya existe, se conserva su contraseña y se le asigna el rol `Admin` si aún no lo tiene. Las cuentas nuevas registradas desde `/cuenta/registro` tienen el rol `Consulta`. Desde el menú **Usuarios**, un administrador puede asignar los roles `Admin`, `Encargado` o `Consulta`; los formularios de escritura requieren rol `Admin` o `Encargado`.

## API JWT

`POST /api/auth/token` recibe JSON con `email` y `password` y devuelve un token Bearer. Úsalo en `GET /api/inventario/productos`, que acepta `search`, `page` y `pageSize`:

```http
Authorization: Bearer <access_token>
```

En Desarrollo, si no se configura una clave JWT, se genera una clave temporal al iniciar la aplicación; por eso los tokens dejan de ser válidos al reiniciarla. En otros entornos configura una clave estable de al menos 32 caracteres como secreto y no la guardes en el repositorio:

```powershell
dotnet user-secrets set "Jwt:SigningKey" "<secreto-aleatorio-de-al-menos-32-caracteres>"
```

## Recuperación de contraseña

No está habilitada todavía: requiere configurar un proveedor de correo para enviar de forma segura los enlaces de restablecimiento de Identity.
