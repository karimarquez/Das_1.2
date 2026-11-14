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

Para crear un administrador local, configura sus credenciales en User Secrets (elige tu propia contraseña segura):

```powershell
dotnet user-secrets set "Auth:BootstrapAdminEmail" "admin@local.test"
dotnet user-secrets set "Auth:BootstrapAdminPassword" "<tu-contraseña-segura>"
```

Al iniciar la aplicación, ese usuario recibe el rol `Admin`. Desde el menú **Usuarios**, un administrador puede asignar los roles `Admin`, `Encargado` o `Consulta`. Los formularios de escritura requieren rol `Admin` o `Encargado`.

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
