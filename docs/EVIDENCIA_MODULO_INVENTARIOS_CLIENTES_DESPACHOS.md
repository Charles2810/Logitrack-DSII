# Evidencia — Módulo de Inventarios, Clientes y Despachos

## 1. Propósito

> **Desarrollo del Módulo de Inventarios, Clientes y Despachos.**<br>
> Programar la interfaz gráfica de usuario y la lógica de negocio con consultas relacionales (CRUD), garantizando transacciones seguras en el control de mercancía.

**Evidencia:** módulo funcional de gestión de productos, stock y registros de despacho operando en el sistema.

Este documento describe la implementación real, con rutas, reglas, transacciones y resultados verificados.

---

## 2. Arquitectura y consultas relacionales

- Modelo **hosted**: `POS.Server` sirve la API y los archivos de `POS.Client` en el mismo origen: `http://localhost:5163` (`README_LogiTrack.md:17`).
- Frontend: **Blazor WebAssembly + MudBlazor 9.12.0**. Backend: **ASP.NET Core Web API + EF Core**. Base: **SQL Server `LogiTrackDB`**.
- Esquema: **18 tablas, 2 vistas y 23 triggers** (`README_LogiTrack.md:25-27`, `README_LogiTrack.md:710`), creado por `database.sql`.
- Acceso relacional con EF Core en `POS.Server/Data/AppDbContext.cs`:
  - `Include` para `HitoActual`, `HojaRuta`, `Detalles`, `Usuario`, `Almacen`, `Producto`.
  - Filtros `Where` por rol, cliente, producto, almacén, hito, fechas y paginación.
  - Proyecciones tipadas en `POS.Server/Infrastructure/GuiaQueries.cs` y en los controladores.
  - `AsNoTracking()` en consultas de solo lectura.
- Mapeo explícito de triggers con `HasTrigger(...)` para evitar el error `334` de EF Core con `OUTPUT`.

---

## 3. Interfaz gráfica de usuario

| Módulo | Ruta | Acciones UI | Confirmación / control | Código |
|---|---|---|---|---|
| Clientes | `/clientes` | **Nuevo cliente**, **Delete** | `Sí / No`; bloquea si el cliente tiene guías | `POS.Client/Pages/Clientes.razor:18,50,120` |
| Productos | `/flota` → pestaña Productos | **Nuevo producto**, **Delete** | `Sí / No`; bloquea si hay stock, movimientos o detalles | `POS.Client/Pages/Flota.razor:73,94,248` |
| Vehículos | `/flota` → pestaña Vehículos | **Nuevo vehículo**, **Delete** | `Sí / No`; el `Delete` **no borra**, hace `PATCH /estado` a `Inhabilitado` | `POS.Client/Pages/Flota.razor:28,57,193` |
| Guías | `/guias` | Crear, editar, asignar hoja, **Delete** | `Delete` solo visible en guías `Registrada`; confirmación con `Sí / No` | `POS.Client/Pages/Guias.razor:20,106-109,113,263` |
| Detalle de guía | `/guias/{id}` | Quitar producto del detalle con icono de papelera | Confirmación `Sí / No` | `POS.Client/Dialogs/GuiaDialog.razor:112,217` |
| Despacho | `/guias/{id}` | **Registrar checkpoint**, **Reportar incidencia**, **Confirmar entrega** | Botones deshabilitados según hito | `POS.Client/Pages/GuiaDetalle.razor:54-70` |
| Existencias | `/inventario` | **Registrar movimiento**, **Movimiento** por fila | Muestra stock disponible antes de registrar | `POS.Client/Pages/Inventario.razor:21,91-92` |
| Movimientos | `/inventario/movimientos` | Filtros por almacén, producto, tipo y fechas | Historial paginado de entradas/salidas | `POS.Client/Pages/Movimientos.razor:24-59` |
| Diálogo de movimiento | — | Tipo `ENTRADA`/`SALIDA`, almacén, producto, cantidad, guía opcional | Valida `SALIDA > stock` y muestra error en snackbar | `POS.Client/Dialogs/MovimientoDialog.razor:58,99,136` |
| Notificaciones | `/notificaciones` | Filtro **Rutas asignadas**, botón **Ver ruta** | Enlace directo a `/hojas-ruta/{id}` | `POS.Client/Pages/Notificaciones.razor:33,77` |
| Rastreo público | `/rastreo` | Consulta por código `LT-...` | Sin datos personales | `POS.Client/Pages/Rastreo.razor` |

Textos exactos de confirmación:

- Cliente: `¿Desea eliminar al cliente {RazonSocial}? Esta acción no se puede deshacer.`
- Producto: `¿Desea eliminar el producto {Sku} · {Nombre}? Esta acción no se puede deshacer.`
- Guía: `¿Desea eliminar la guía {CodigoGuia}? Solo es posible si está Registrada y no tiene eventos de trazabilidad.`
- Vehículo: `¿Desea inhabilitar el vehículo {Placa}? No se elimina: pasa a estado Inhabilitado.`
- Línea de detalle: `¿Desea eliminar {SKU} del detalle de la guía?`

Todas usan `DialogService.ShowMessageBoxAsync` con botones **Sí / No**.

---

## 4. Lógica de negocio y CRUD relacional

| Entidad | Endpoints | Reglas exactas | Código |
|---|---|---|---|
| Clientes | `GET/POST/PUT /api/clientes`, `DELETE /api/clientes/{id}` | `DELETE` → `404` si no existe; `409` si tiene `GuiaEnvio`. Roles: Admin Sistema + Admin Logístico | `POS.Server/Controllers/ClientesController.cs:92,99` |
| Productos | `GET/POST/PUT /api/productos`, `DELETE /api/productos/{id}` | `DELETE` → `409` si existe `Inventario`, `MovimientoInventario` o `DetalleGuia` | `POS.Server/Controllers/ProductosController.cs:89,96-98` |
| Guías | `GET/POST/PUT`, `PATCH /{id}/hoja-ruta`, `DELETE /{id}` | Alta solo Admin Logístico y valida stock del almacén de origen por producto; `DELETE` exige hito `Registrada` y sin checkpoints, incidencias, entregas, movimientos ni notificaciones | `POS.Server/Controllers/GuiasController.cs:232-275,384,407,417-426` |
| Inventario | `GET /api/inventario`, `GET /alertas`, `GET /movimientos`, `POST /movimientos` | Stock por almacén+producto; tipo solo `ENTRADA`/`SALIDA`; RN14 impide stock negativo | `POS.Server/Controllers/InventarioController.cs:25,58,77,134` |
| Checkpoints | `POST /api/checkpoints` | Hito siguiente (+1), guía asignada, hoja no finalizada, RN13 por rol; dispara descuento automático | `POS.Server/Controllers/CheckpointsController.cs:35-133` |
| Incidencias | `POST /api/incidencias` | Requiere descripción y foto; solo conductor/supervisor de su ruta | `POS.Server/Controllers/IncidenciasController.cs:94` |
| Entregas | `POST /api/entregas` | Requiere firma o foto; hito `En Reparto Final`; conductor asignado | `POS.Server/Controllers/EntregasController.cs:88-101` |
| Usuarios | `PATCH /{id}/estado` | **No existe DELETE** (RN11): solo activar/inhabilitar | `POS.Server/Controllers/UsuariosController.cs:128` |
| Vehículos | `PATCH /{id}/estado` | El botón `Delete` de UI hace baja lógica a `Inhabilitado` | `POS.Server/Controllers/VehiculosController.cs:116` |
| Notificaciones | `GET /api/notificaciones`, `PATCH /{id}/leida` | Filtradas por `id_usuario` del JWT; incluyen `IdHojaRuta` para el acceso directo | `POS.Server/Controllers/NotificacionesController.cs:22,52,94` |

Respuestas de error: contrato `ApiError { codigo, mensaje, detalle, traza }`. Los errores SQL de los triggers se traducen a mensajes legibles (por ejemplo, RN03, RN04, RN05, RN14).

---

## 5. Transacciones seguras en el control de mercancía

### 5.1 Atomicidad

- Cada `SaveChangesAsync()` se ejecuta dentro de una transacción de EF Core.
- Transacciones **explícitas** donde hay más de un `SaveChangesAsync()`:
  - Reemplazo de detalle de guía: `POS.Server/Controllers/GuiasController.cs:355-371`.
  - Baja de guía: elimina `DetalleGuia` y luego `GuiaEnvio`, con `CommitAsync()`: `POS.Server/Controllers/GuiasController.cs:431-438`.
  - Checkpoint de salida + movimientos + descuento: `POS.Server/Controllers/CheckpointsController.cs:112-133`.

### 5.2 Descuento automático al salir el camión

Al registrar el checkpoint **Salida de Almacén** (`Hitos.SalidaAlmacen`):

1. `CheckpointsController.cs:85-101` valida stock por cada producto del detalle en el almacén de origen de la guía.
2. `CheckpointsController.cs:112-133` abre transacción, inserta el checkpoint y un `MovimientoInventario` de tipo `SALIDA` por cada detalle, con `IdGuia`, `IdAlmacenOrigen`, `IdProducto`, `Cantidad` e `IdUsuario`.
3. El trigger `trg_movimiento_inventario` (`database.sql:1018-1079`) descuenta `stock_actual` y ejecuta `ROLLBACK TRANSACTION` + `THROW 50016` si el stock sería negativo.
4. `CommitAsync()` confirma todo junto. Si falla el stock, **no queda checkpoint ni movimiento**.

No debe registrarse además un movimiento manual `SALIDA` para esa guía: duplicaría el descuento.

### 5.3 Triggers relevantes

| Trigger | Ubicación | Función |
|---|---|---|
| `trg_hoja_ruta_notifica` | `database.sql:461` | Genera `Nueva ruta asignada` con `id_hoja_ruta` |
| `trg_guia_envio_asigna_notifica` | `database.sql:654` | Genera `Nueva guía asignada` al asignar una guía |
| `trg_checkpoint_valida` | `database.sql:678` | Valida RN05/RN06/RN13 y avanza el hito |
| `trg_movimiento_inventario` | `database.sql:1018-1079` | Actualiza stock y bloquea stock negativo (RN14) |
| `trg_movimiento_inmutable` | `database.sql:952` | Prohíbe modificar/borrar movimientos (RNF05) |
| `trg_usuario_no_delete`, `trg_vehiculo_no_delete` | `database.sql:340,985` | RN11: baja lógica, no borrado físico |

---

## 6. Evidencia de ejecución verificada

### 6.1 Compilación y colección

```powershell
dotnet build POS.Server\POS.Server.csproj --nologo -p:OutputPath=C:\Users\humbe\AppData\Local\Temp\opencode\logitrack-server\
```

Resultado: **0 advertencias, 0 errores**.

Colección Postman: **17 carpetas / 68 peticiones**, JSON válido (`Postman_LogiTrack.json`).

### 6.2 Descuento real por salida de camión

Flujo ejecutado sobre la API en ejecución:

1. Crear guía `LT-00000009` con producto `SKU-0004`, cantidad `2`, almacén de origen `1`.
2. Asignar a la hoja de ruta `1` (Carlos).
3. Carlos registra checkpoint `Salida de Almacén` → `idCheckpoint = 16`.

Resultado consultado en SQL Server:

```text
stock_actual SKU-0004 antes: 109
stock_actual SKU-0004 después: 107
movimiento_inventario: id=10, tipo=SALIDA, cantidad=2, id_guia=9
```

### 6.3 Rollback por stock insuficiente

Prueba negativa: guía con producto `SKU-0005`, cantidad `999`, stock disponible `50`.

Respuesta de la API:

```json
{
  "codigo": 400,
  "mensaje": "Stock insuficiente en el almacén de origen para el producto 5: disponible 50, requerido 999.",
  "detalle": null,
  "traza": null
}
```

Verificación en BD: stock `50` sin cambios, `0` checkpoints y `0` movimientos para esa guía.

### 6.4 Notificaciones de ruta

Tras aplicar el parche de `id_hoja_ruta` y el nuevo tipo:

```text
Nueva ruta asignada | 2
Nueva guía asignada | 4
Incidencia          | 2
Entrega finalizada  | 2
```

Las notificaciones de ruta tienen `id_hoja_ruta = 1` y `2`, y muestran el botón **Ver ruta** → `/hojas-ruta/{id}`.

### 6.5 Estado de datos al momento de la verificación

```text
guías en BD:          9
movimientos en BD:   10
stock almacén 1:      SKU-0001=242, SKU-0002=78, SKU-0003=476, SKU-0004=107, SKU-0005=48
```

Los valores pueden variar al continuar probando la aplicación.

---

## 7. Reproducción del flujo

```powershell
cd D:\Projects\Logitrack-DSII
Get-Process -Name POS.Server -ErrorAction SilentlyContinue | Stop-Process -Force
sqlcmd -S localhost -E -b -f 65001 -i database.sql
dotnet run --project POS.Server --launch-profile http
```

Abrir `http://localhost:5163` y seguir:

1. **Ana** (`ana.rojas@logitrack.test` / `LogiTrack2026!`): crear cliente y producto; registrar una `ENTRADA`.
2. **Ana**: crear guía con ese producto y asignarla a la **Hoja 1**.
3. **Carlos** (`carlos.soliz@logitrack.test`): abrir la guía y registrar **Salida de Almacén**.
4. Verificar en **Existencias** el stock descontado y en **Movimientos** la `SALIDA` vinculada a la guía.
5. Continuar con incidencia, entrega, rastreo público, notificaciones y reportes.
6. Probar bajas: `Delete` de cliente/producto con relaciones → `409`; guía `Registrada` sin eventos → se elimina; vehículo `Delete` → pasa a `Inhabilitado`.

Para repetir desde cero, detener la app y volver a cargar `database.sql`.

---

## 8. Conclusión

El módulo queda funcional en sus tres frentes:

- **Interfaz gráfica**: altas, ediciones, bajas controladas, confirmaciones, filtros y accesos directos por rol.
- **Lógica relacional**: CRUD con validaciones de existencia, FK, roles y reglas de negocio en API y base de datos.
- **Transacciones seguras**: operaciones multi-tabla y descuento de mercancía confirmados de forma atómica, con rollback verificado ante stock insuficiente.
