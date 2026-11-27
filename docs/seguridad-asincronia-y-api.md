# Etapa 4 · Protección, comunicación asíncrona y documentación de la API

Para cada punto indica qué se implementó, en qué archivo está y qué prueba automatizada lo comprueba. La API REST de productos, Identity, roles y JWT (puntos 1 y 2) ya existían. aquí se protegen, se consumen desde el sitio y se documentan.

## Resumen

| Punto | Qué se agregó |
|---|---|
| 3. Protección | Token antifalsificación obligatorio en todo formulario y en las llamadas AJAX, política de seguridad de contenido (CSP) y encabezados de seguridad, validación de texto plano contra HTML en cliente y servidor, validación de la API de productos, límite de intentos de inicio de sesión y de token |
| 4. Comunicación asíncrona | Seis endpoints JSON y una vista parcial que usa el propio sitio, cliente Fetch común (`site.js`), registro de movimientos con Fetch, conteo rápido con jQuery AJAX, preparación de recetas con Fetch, tablero que se actualiza sin recargar (JSON y vista parcial) |
| 5. Documentación y pruebas | Swagger (Swashbuckle 10.3.1) con JWT, cookie de sesión y encabezado CSRF documentados, archivo `DAS.Web.http`, 93 pruebas nuevas (190 en total) |

## 3. Protección de la aplicación

### 3.1 Protección CSRF

- **Formularios MVC.** `Security/ViewControllersAntiforgeryConvention.cs` agrega `AutoValidateAntiforgeryToken` a todos los controladores con vistas. Cualquier acción POST nueva queda protegida aunque se olvide `[ValidateAntiForgeryToken]`. Las páginas de error (`HomeController`) declaran `[IgnoreAntiforgeryToken]` para poder mostrarse cuando falla la validación.
- **Llamadas AJAX y Fetch.** El layout publica el token en `<meta name="csrf-token">` y el nombre del encabezado en `<meta name="csrf-header">`. `site.js` lo envía en `X-CSRF-TOKEN` en toda petición que no sea GET. Los controladores de `Controllers/Api` que usan la cookie de sesión declaran `[AutoValidateAntiforgeryToken]`.
- **Cookie antifalsificación.** `HttpOnly`, `SameSite=Strict` y `Secure` cuando la petición llega por HTTPS (`Extensions/ProtectionExtensions.cs`).
- **API con JWT.** No usa cookies: el navegador no envía el token Bearer por su cuenta, así que no hay CSRF posible y esos endpoints no piden el encabezado.

Pruebas: `SecurityTests.AccionesPostDeLasVistas_ExigenTokenAntifalsificacion` recorre por reflexión todas las acciones POST de las vistas; `FormularioSinTokenAntifalsificacion_SeRechazaSinCambiarDatos`, `ApiDeSesionSinEncabezadoCsrf_SeRechazaAunqueExistaLaCookie`, `ApiDeSesionConTokenDeOtraSesion_SeRechaza` y `ProductApiTests.ApiJwt_NoDependeDelTokenAntifalsificacion`.

### 3.2 Protección XSS

- **Codificación de salida.** Razor codifica todo lo que escribe; ninguna vista usa `Html.Raw`. Las respuestas JSON escapan `<`, `>`, `&` y comillas (`JavaScriptEncoder.Create(UnicodeRanges.All)`): por defecto ASP.NET Core usa un codificador relajado que deja pasar `<script>`. Las tildes y la ñ se siguen escribiendo tal cual.
- **Política de seguridad de contenido.** `Security/SecurityHeadersMiddleware.cs` envía en cada respuesta:

  ```text
  Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:;
                           font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self';
                           form-action 'self'; frame-ancestors 'none'
  ```

  Para que la política funcione sin `unsafe-inline`, el script que estaba dentro de `Inventory/Index.cshtml`, el `onsubmit` de recetas y los atributos `style` se movieron a `wwwroot/js` y `site.css`. La barra de existencia usa clases `stock-w-0` a `stock-w-100` en lugar de `style="width"`. Swagger UI recibe la misma política con `style-src 'unsafe-inline'`, que necesita su interfaz.
- **Entrada de texto plano.** `Application/Validation/PlainTextAttribute.cs` rechaza `<` y `>` en nombres, presentaciones, ubicaciones, motivos y descripciones. Se aplica a los ViewModels y a `SaveProductDto`.
- **Cliente.** El JavaScript escribe datos solo con `textContent`. La vista parcial de actividad se inserta con `DOMParser`, que no ejecuta scripts, y además viene codificada por Razor.

Pruebas: `SecurityTests.Respuestas_IncluyenEncabezadosDeSeguridad`, `Vistas_NoUsanScriptsEstilosNiManejadoresEnLinea`, `TextoConEtiquetas_SeCodificaEnHtmlYEnJson`, `FormularioDeProducto_RechazaEtiquetasHtmlEnElServidor`.

### 3.3 Validación del lado del servidor

- Los ViewModels mantienen sus DataAnnotations e `IValidatableObject`; se agregó `[PlainText]` y `[Required]` explícito donde el mensaje salía en inglés.
- `SaveProductDto`, que la API JWT recibe directamente, no tenía validaciones: se podían crear productos sin nombre, con costo negativo o con categorías fuera del catálogo. Ahora declara las mismas reglas que el formulario, incluidas catálogo y equivalencia de unidades. `[ApiController]` responde 400 automáticamente.
- `ProductService` valida además el DTO con `InputValidator` antes de guardar: el caso de uso no depende de que el controlador recuerde revisar `ModelState`.
- Los mensajes de `LoginViewModel`, `RegisterViewModel` y `TokenRequest` están en español.
- Los errores de la API se devuelven como `ValidationProblemDetails` (`application/problem+json`, RFC 9457) con el error junto a su campo y títulos en español.

Pruebas: `ValidationTests` (servidor y API), `ProductApiTests.CrearProductoInvalido_Responde400ConErroresPorCampo`, `CrearProductoFueraDelCatalogo_Responde400`, `InputValidationTests` (unitarias).

### 3.4 Validación del lado del cliente

- jQuery Validation Unobtrusive ahora también se carga en inicio de sesión, registro, tablero (modales de movimiento y conteo) y recetas.
- Los campos de los modales se generan con `asp-for`, así que el cliente aplica las mismas reglas y mensajes que el ViewModel del servidor.
- `wwwroot/js/validation.js` registra el adaptador `plaintext` (atributo `data-val-plaintext` que genera `Models/Validation/PlainTextAttributeAdapter.cs`) y traduce los mensajes por defecto de jQuery Validation.
- En las páginas de acceso los mensajes tienen espacio reservado: al corregir un campo el botón no se mueve y el clic no se pierde.

Pruebas: `ValidationTests.Login_IncluyeValidacionDelLadoDelClienteEnEspanol`, `ModalDeMovimiento_GeneraLasReglasDelViewModelParaElCliente`, `FormularioDeProducto_IncluyeElAdaptadorDeTextoPlano`.

### 3.5 Manejo seguro de credenciales

- Identity guarda las contraseñas con hash y bloquea la cuenta 10 minutos tras 5 intentos fallidos (configuración existente).
- **Límite por equipo.** `POST /cuenta/ingresar`, `POST /cuenta/registro` y `POST /api/auth/token` aceptan 10 intentos por minuto por dirección IP (`[EnableRateLimiting]`). Esto frena que alguien pruebe contraseñas contra muchas cuentas distintas. Al superarlo, la API responde 429 en JSON con `Retry-After` y el sitio muestra «Demasiados intentos». El límite se configura con `Seguridad:IntentosDeCredencialesPorMinuto`.
- **Sitio sin tokens en el navegador.** El sitio no guarda tokens: usa la cookie de Identity (`HttpOnly`) más el token CSRF. Swagger no persiste el JWT entre recargas.
- **Secretos fuera del repositorio.** La clave JWT y el administrador inicial siguen en User Secrets. La contraseña para `DAS.Web.http` va en `http-client.env.json.user`, que `.gitignore` ya excluye (`*.user`).

Pruebas: `SecurityTests.SolicitudesDeTokenJwt_SeLimitanPorMinuto`, `IntentosDeInicioDeSesion_SeLimitanPorMinuto`, `CookieAntifalsificacion_EsHttpOnlySameSiteStrictYSegura`, `ProductApiTests.TokenAlterado_Responde401ConDesafioBearer`.

### 3.6 Buenas prácticas de seguridad

| Medida | Dónde |
|---|---|
| `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy` | `SecurityHeadersMiddleware` |
| Sin encabezado `Server: Kestrel` | `ProtectionExtensions` (`AddServerHeader = false`) |
| La API responde 401/403 en JSON en lugar de redirigir al inicio de sesión o devolver una página HTML | `ProtectionExtensions.UseStatusCodeResponses` |
| HTTPS y HSTS fuera de Desarrollo, `LocalRedirect` y `[BindNever]` contra redirección abierta y sobreasignación | Ya existían |
| Respuestas JSON del tablero y de la actividad sin caché (`Cache-Control: no-store`) | `[ResponseCache]` en las acciones |

## 4. Comunicación asíncrona

### Endpoints que consume el sitio

Usan la cookie de la sesión iniciada. Los que modifican datos exigen el encabezado `X-CSRF-TOKEN` y el rol Admin o Encargado.

| Método | Ruta | Uso en el sitio | Respuesta |
|---|---|---|---|
| GET | `/api/tablero` | Indicadores y existencias del tablero | 200 `DashboardResponse` |
| POST | `/api/movimientos` | Modal «Registrar movimiento» (Fetch) | 201 movimiento + existencia actualizada · 400 errores por campo |
| POST | `/api/conteos` | Modal «Conteo físico» (jQuery AJAX) | 201 conteo + existencia; `Location: /api/conteos/{id}` |
| GET | `/api/conteos/{id}` | Resultado de un conteo | 200 · 404 |
| GET | `/api/recetas` | Recetas con ingredientes | 200 |
| POST | `/api/recetas/{id}/preparaciones` | Botón «Registrar preparación» (Fetch) | 201 ingredientes descontados · 400 · 404 |
| GET | `/inventario/actividad` | Panel «Actividad» | Vista parcial HTML `_RecentActivity` |

### Cómo fluye un registro

```mermaid
sequenceDiagram
    actor U as Encargado
    participant JS as inventory.js
    participant API as MovementsApiController
    participant S as MovementService
    U->>JS: Guardar movimiento
    JS->>JS: Valida con jQuery Validation (reglas del ViewModel)
    JS->>API: POST /api/movimientos (JSON, cookie, X-CSRF-TOKEN)
    API->>API: Autorización por rol, antifalsificación, validación del modelo
    API->>S: RegisterAsync en una transacción
    S-->>API: ServiceResult
    API-->>JS: 201 JSON {movement, product} o 400 ProblemDetails
    JS->>JS: Actualiza fila, barra y estado; muestra el aviso
    JS->>API: GET /api/tablero y GET /inventario/actividad
    JS->>JS: Actualiza indicadores y reemplaza el panel de actividad
```

### Archivos del cliente

| Archivo | Contenido |
|---|---|
| `wwwroot/js/site.js` | `window.pipiris`: cliente Fetch con JSON, token CSRF, `credentials: 'same-origin'` y manejo de errores por código HTTP; avisos flotantes accesibles; ayudas para formularios (validar, mostrar errores del servidor junto a cada campo, estado ocupado); confirmación con `data-confirm` |
| `wwwroot/js/inventory.js` | Búsqueda y filtro locales, movimiento con Fetch, conteo con `$.ajax`, refresco manual y automático cada 60 s, solo con la pestaña visible y sin modales abiertos |
| `wwwroot/js/recipes.js` | Preparación de recetas con Fetch y resultado dentro de la tarjeta |
| `wwwroot/js/validation.js` | Mensajes de jQuery Validation en español y adaptador `plaintext` |

Interacción cliente-servidor:

| Respuesta | Qué hace el cliente |
|---|---|
| 400 | Muestra cada error junto a su campo y, si no tiene campo, en el resumen del formulario |
| 401 | Avisa que la sesión terminó y recarga la página para volver al inicio de sesión |
| 403 | Muestra el aviso «No tienes permiso…» |
| 429 | Pide esperar |
| Sin conexión | Avisa que no hay conexión con el servidor |

Durante el envío el botón se deshabilita y queda con `aria-busy`.

Mejora progresiva: los formularios conservan `action` y `method`. Sin JavaScript se envían como antes y el servidor responde con redirección y mensaje.

Pruebas: `AsyncEndpointsTests` (15 casos), más el recorrido en navegador descrito en el punto 5.6.

## 5. Documentación y pruebas de API

### 5.1 Swagger

- Paquetes `Swashbuckle.AspNetCore.SwaggerGen` y `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1, la versión para .NET 10.
- Interfaz en `/swagger` y documento en `/swagger/v1/swagger.json`. Se activa en Desarrollo y en pruebas; en otro entorno se habilita con `Swagger:Habilitado=true`.
- **Botón Authorize.** Ofrece el esquema `Bearer` (JWT). Las operaciones de sesión web declaran el esquema `SesionWeb` (cookie). Los GET de sesión se pueden probar desde la misma pestaña si ya se inició sesión en el sitio.
- Solo se documentan las rutas `/api/...`, agrupadas en siete secciones con descripción.
- Configuración: `Extensions/ApiDocumentationExtensions.cs` y la carpeta `OpenApi/`.

### 5.2 Documentación de endpoints

- Se incluyen los comentarios XML que ya tenía `ApiProductsController` (`GenerateDocumentationFile` en `DAS.Web.csproj`).
- Los endpoints nuevos se describen con `[EndpointSummary]`, `[EndpointDescription]`, `[Tags]` y `[ProducesResponseType]`, sin comentarios en el código.
- `SecurityRequirementsOperationFilter` marca qué esquema protege cada operación y documenta 401 y 403. `AntiforgeryHeaderOperationFilter` agrega el parámetro obligatorio `X-CSRF-TOKEN` a las operaciones de sesión que modifican datos.
- Los esquemas reflejan las validaciones (longitudes, rangos, requeridos) y los enums se documentan como texto (`"Consumo"`).

| Sección | Endpoints |
|---|---|
| Autenticación (JWT) | `POST /api/auth/token` |
| Productos (JWT) | `GET/POST /api/inventario/productos`, `GET/PUT/DELETE /api/inventario/productos/{id}` |
| Productos (sesión web) | `GET /api/productos?bajoMinimo=true`, `GET /api/productos/{id}` |
| Tablero, Movimientos, Conteos, Recetas (sesión web) | Tabla del punto 4 |

### 5.3 Pruebas de servicios

- **Prueba manual.** `src/DAS.Web/DAS.Web.http` (Visual Studio 2022 17.12 o posterior) encadena token → listar → crear → consultar → actualizar → desactivar, más casos de error: datos inválidos, sin token y token alterado. Antes de usarlo, crea `src/DAS.Web/http-client.env.json.user` con tu contraseña local y elige el entorno `dev`:

  ```json
  { "dev": { "password": "<tu-contraseña>" } }
  ```

- **Pruebas automáticas.** `ProductApiTests` recorre la API JWT completa con usuarios reales de Identity y tokens emitidos por `/api/auth/token`. `AsyncEndpointsTests` recorre los endpoints de sesión.

### 5.4 Validación de respuestas

Las pruebas comprueban:

- el código HTTP (200, 201, 204, 400, 401, 403, 404, 429);
- el `Content-Type`: `application/json` en respuestas correctas y `application/problem+json` en errores;
- el encabezado `Location` de los recursos creados y el `WWW-Authenticate: Bearer` del token alterado;
- la forma del JSON: nombres y tipos de las propiedades, ausencia de costos en el tablero, errores agrupados por campo.

`ApiDocumentationTests` verifica que el documento OpenAPI describa cada endpoint con resumen, etiqueta, respuestas, esquemas de seguridad y encabezado CSRF.

### 5.5 Consistencia de datos

- `ProductApiTests.Existencia_EsConsistenteEntreApisBaseDeDatosYKardex` crea un producto por la API JWT y le registra un consumo, una entrada, una merma y un conteo por la API de sesión. Luego comprueba:
  - que la existencia coincide en la base, la API JWT, la API de sesión y el tablero;
  - que cada movimiento del kardex empieza donde terminó el anterior;
  - que la suma de los cambios es igual a la existencia.
- `AsyncEndpointsTests.PrepararRecetaSinExistenciaSuficiente_NoDescuentaNingunIngrediente` comprueba que una operación rechazada no deja cambios parciales.
- Las demás pruebas de escritura comparan la respuesta con lo guardado en la base.

### 5.6 Pruebas funcionales

| Clase | Casos | Qué cubre |
|---|---|---|
| `Functional/SecurityTests` | 15 | Encabezados, CSP, CSRF en formularios y API, XSS, cookies, límite de intentos |
| `Functional/ValidationTests` | 7 | Validación de cliente generada, validación de servidor, ProblemDetails |
| `Functional/AsyncEndpointsTests` | 15 | Endpoints JSON del sitio, roles, 401 en JSON, vista parcial |
| `Functional/ProductApiTests` | 10 | API JWT de punta a punta, errores, contrato JSON, consistencia |
| `Functional/ApiDocumentationTests` | 26 | Documento OpenAPI e interfaz Swagger |
| `Unit/Application/InputValidationTests` | 16 | `PlainText`, reglas de `SaveProductDto`, validación en el servicio |
| `Unit/Application/DashboardServiceTests` | 4 | Indicadores del tablero con repositorios en memoria |

Además se recorrió el sitio en Chrome sin interfaz (Playwright) con la CSP activa. Se comprobó:

- validación de cliente en inicio de sesión, registro, modales, recetas y producto;
- error del servidor junto al campo;
- registro de movimiento, conteo y preparación sin recargar la página;
- refresco del tablero;
- Swagger con JWT y con la cookie de sesión;
- cero violaciones de CSP en la consola.

```powershell
dotnet test
dotnet test --filter "FullyQualifiedName~DAS.Tests.Functional"
dotnet test --filter "FullyQualifiedName~ApiDocumentationTests|FullyQualifiedName~ProductApiTests"
```

## Archivos compartidos que se modificaron

Para revisar al integrar con las otras partes:

| Archivo | Cambio |
|---|---|
| `Program.cs` | Registra `AddApplicationProtection` y `AddApiDocumentation`. Agrega `UseSecurityHeaders`, `UseStatusCodeResponses` (reemplaza a `UseStatusCodePagesWithReExecute`), `UseApiDocumentation` y `UseRateLimiter` |
| `ApiAuthController`, `AccountController` | `[EnableRateLimiting]` en los POST de credenciales y atributos de documentación en el token |
| `ProductsApiController` | Atributos de documentación |
| `InventoryController` | El tablero usa el nuevo `IDashboardService`; nueva acción `actividad` |
| `HomeController` | `[IgnoreAntiforgeryToken]` y mensaje para el error 429 |
| `SaveProductDto`, `ProductService` | Validación del DTO de la API |
| `DasWebApplicationFactory` (pruebas) | Amplía el límite de intentos para que las pruebas no se bloqueen entre sí |

