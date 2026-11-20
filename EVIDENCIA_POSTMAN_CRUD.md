# Evidencia — CRUD demostrado en Postman

Documento rápido para mostrar al docente CRUD reales sobre la API de LogiTrack. Usa la colección `Postman_LogiTrack.json` (17 carpetas / 68 peticiones).

## 1. Preparación

1. Importar `Postman_LogiTrack.json` en Postman.
2. Verificar variable de colección:
   - `baseUrl = http://localhost:5163`
3. Ejecutar primero:
   - `01 Autenticación` → `Login - Administrador Logístico (Ana)`
   - El script **Tests** guarda el token en `tokenAdminLogistico`.
4. Ejecutar la app si no está activa:

```powershell
cd D:\Projects\Logitrack-DSII
dotnet run --project POS.Server --launch-profile http
```

> Los IDs mostrados abajo son de una ejecución real; pueden cambiar al repetir. Usa NIT/SKU nuevos para evitar conflictos.

---

## 2. Demo A — Cliente: CRUD completo

Carpeta: `13 Clientes`.

| Paso | Petición Postman | Método y URL | Body / dato | Resultado capturado |
|---|---|---|---|---|
| 1 | `Crear cliente (guarda idCliente)` | `POST {{baseUrl}}/api/clientes` | `{ "razonSocial":"Cliente Demo Postman", "nit":"NIT-PM-193723", "telefono":"77777777", "direccion":"Av. Demo 123" }` | **201 Created** · `idCliente = 5` |
| 2 | `Listar clientes` | `GET {{baseUrl}}/api/clientes?buscar=NIT-PM-193723` | — | **200 OK** · `total = 1` · `razonSocial = Cliente Demo Postman` |
| 3 | `Actualizar cliente` | `PUT {{baseUrl}}/api/clientes/{{idCliente}}` | `{ "razonSocial":"Cliente Demo Postman Editado", "nit":"NIT-PM-193723", "telefono":"88888888", "direccion":"Av. Demo 456" }` | **200 OK** · `telefono = 88888888` |
| 4 | `Eliminar cliente` | `DELETE {{baseUrl}}/api/clientes/{{idCliente}}` | — | **204 No Content** |
| 5 | `Listar clientes` | `GET {{baseUrl}}/api/clientes?buscar=NIT-PM-193723` | — | **200 OK** · `total = 0` (confirmado eliminado) |

El script de la petición 1 hace:

```javascript
pm.collectionVariables.set('idCliente', String(pm.response.json().idCliente));
```

Por eso las peticiones 3 y 4 usan `{{idCliente}}` automáticamente.

---

## 3. Demo B — Producto: CRUD completo + restricción

Carpeta: `14 Productos`.

| Paso | Petición Postman | Método y URL | Body / dato | Resultado capturado |
|---|---|---|---|---|
| 1 | `Crear producto (guarda idProducto)` | `POST {{baseUrl}}/api/productos` | `{ "sku":"SKU-PM-193754", "nombre":"Producto Demo Postman", "pesoUnitarioKg":1.5, "volumenUnitarioM3":0.02 }` | **201 Created** · `idProducto = 6` |
| 2 | `Listar productos` | `GET {{baseUrl}}/api/productos?buscar=SKU-PM-193754` | — | **200 OK** · `total = 1` · `pesoUnitarioKg = 1.500` |
| 3 | `Actualizar producto` | `PUT {{baseUrl}}/api/productos/{{idProducto}}` | `{ "sku":"SKU-PM-193754", "nombre":"Producto Demo Postman Editado", "pesoUnitarioKg":2.5, "volumenUnitarioM3":0.03 }` | **200 OK** · `pesoUnitarioKg = 2.5` |
| 4 | `Eliminar producto` | `DELETE {{baseUrl}}/api/productos/{{idProducto}}` | — | **204 No Content** |
| 5 | `Listar productos` | `GET {{baseUrl}}/api/productos?buscar=SKU-PM-193754` | — | **200 OK** · `total = 0` |

### Validación de baja segura (mostrar también)

Ejecutar `Eliminar producto` con `{{idProducto}} = 4` (producto con stock y movimientos):

```http
DELETE {{baseUrl}}/api/productos/4
Authorization: Bearer {{tokenAdminLogistico}}
```

Respuesta real:

```json
{
  "codigo": 409,
  "mensaje": "No se puede eliminar el producto: tiene stock, movimientos o detalles de guía asociados.",
  "detalle": null,
  "traza": null
}
```

Esto evidencia que el CRUD no permite borrar datos referenciados.

---

## 4. Demo C — Movimiento de inventario: crear y consultar

Carpeta: `08 Inventario`.

Los movimientos son inmutables (RNF05): tienen `POST` y `GET`, pero **no** `PUT`/`DELETE` por diseño.

| Paso | Petición Postman | Método y URL | Body | Resultado capturado |
|---|---|---|---|---|
| 1 | `Registrar movimiento ENTRADA (guarda idMovimiento)` | `POST {{baseUrl}}/api/inventario/movimientos` | `{ "idAlmacen":1, "idProducto":5, "idGuia":null, "tipoMovimiento":"ENTRADA", "cantidad":5 }` | **201 Created** · `idMovimiento = 12` · `tipo = ENTRADA` |
| 2 | `Consultar inventario` | `GET {{baseUrl}}/api/inventario?idAlmacen=1&buscar=SKU-0005` | — | **200 OK** · stock `48 → 53` |
| 3 | `Listar movimientos` | `GET {{baseUrl}}/api/inventario/movimientos?page=1&pageSize=3` | — | **200 OK** · primer registro `ENTRADA 5` |

El trigger `trg_movimiento_inventario` aplicó el cambio de stock dentro de la misma transacción del `POST`.

---

## 5. Qué señalar al docente en Postman

- Pestaña **Tests** en verde: `Cliente creado (201)`, producto creado, movimiento creado.
- Códigos de estado visibles: **201**, **200**, **204** y **409**.
- En la pestaña **Variables** de la colección: `idCliente`, `idProducto`, `idMovimiento` actualizados por los scripts.
- En **Body** → **Response**: los objetos devueltos con sus IDs reales.
- Para el caso de rollback/transacción, usar además `05 Checkpoints` → `Checkpoint 1 - Salida de Almacén` y verificar stock en `08 Inventario`.

---

## 6. Notas

- Si una petición devuelve **401**, volver a ejecutar `Login - Administrador Logístico (Ana)`.
- El token `tokenAdminLogistico` tiene permisos para clientes, productos, guías e inventario.
- No repetir `Eliminar producto` sobre IDs con stock, movimientos o detalles: el **409** es el comportamiento correcto.
- La evidencia documental completa está en `PRODUCTO_FORMULARIOS_CONTROLADORES_TRANSACCIONES_CRUD.md` y `EVIDENCIA_MODULO_INVENTARIOS_CLIENTES_DESPACHOS.md`.
