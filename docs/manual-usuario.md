# Manual de usuario

## 1. Acceso y roles

Abre la dirección de la aplicación y entra desde **Iniciar sesión**. Las cuentas nuevas tienen el rol **Consulta**: pueden revisar información, pero no registrar cambios. Los roles **Encargado** y **Admin** pueden realizar operaciones; solo **Admin** administra los roles de otras cuentas.

Si aparece **Acceso denegado**, solicita al administrador que revise tu rol. No compartas tu contraseña ni tokens de acceso.

## 2. Inventario y productos

1. Abre **Inventario** para revisar existencias, niveles mínimos y productos que necesitan reposición.
2. Usa la búsqueda o el filtro de categoría para localizar un producto.
3. Abre el detalle para consultar su información e historial.
4. Los usuarios autorizados pueden crear o editar productos. Editar los datos del producto no cambia su existencia.
5. Si un producto ya tiene movimientos, se desactiva en lugar de eliminarse; su historial permanece disponible.

## 3. Registrar movimientos

Desde **Movimientos** o el acceso del producto, selecciona el tipo de operación y completa producto, cantidad, motivo y fecha.

- **Entrada:** aumenta la existencia.
- **Consumo:** descuenta producto utilizado.
- **Merma:** registra una pérdida y su motivo.

Revisa el resumen antes de guardar. La aplicación rechaza cantidades inválidas y consumos que excedan la existencia disponible. El movimiento guarda la cuenta, fecha y saldos para conservar la trazabilidad.

## 4. Recetas

En **Recetas**, abre una receta y registra el número de porciones preparadas. La aplicación calcula y descuenta los ingredientes según la receta. Si falta algún ingrediente, no se completa parcialmente la operación; revisa el mensaje y las existencias antes de volver a intentarlo.

Los usuarios autorizados pueden crear, editar o eliminar recetas según sus permisos.

## 5. Conteos físicos

1. Abre el conteo desde Inventario.
2. Ingresa únicamente las cantidades que contaste físicamente.
3. Revisa los datos y guarda el conteo.
4. Consulta el resultado para ver diferencias y ajustes registrados.

El saldo teórico se oculta durante la captura para evitar influir en el conteo. No repitas un producto en el mismo conteo.

## 6. Reportes

En **Reportes**, elige un día o un mes para consultar entradas, consumos, mermas, ajustes y costos. Puedes exportar el período a CSV para abrirlo en una hoja de cálculo.

## 7. Errores y ayuda

Si aparece una página de error, anota el **Código de la solicitud** y envíalo al administrador junto con la hora aproximada y la acción que estabas realizando. El código ayuda a localizar el evento en los logs; no envíes contraseñas ni datos de sesión.
