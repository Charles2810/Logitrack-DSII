# Producto — Formularios, controladores y transacciones CRUD con conexión persistente a la Base de Datos

## 1. Enunciado e interpretación

> **Documento y desarrolla formularios, controladores y lógica de transacciones CRUD con conexión persistente a la Base de Datos.**

Se interpreta como la evidencia de cuatro capas conectadas:

1. **Formularios**: interfaz Blazor/MudBlazor para crear, editar, asignar, inhabilitar y eliminar entidades.
2. **Controladores**: API ASP.NET Core que expone CRUD/HTTP, aplica roles y valida reglas de negocio.
3. **Lógica de transacciones**: `SaveChangesAsync()` atómico y bloques explícitos `BeginTransactionAsync`/`CommitAsync` cuando intervienen varias tablas; los triggers de SQL Server refuerzan las reglas y descuentan stock.
4. **Conexión persistente**: `AppDbContext` enlazado a SQL Server `LogiTrackDB`; los datos sobreviven al reinicio de la aplicación.

Este documento complementa `EVIDENCIA_MODULO_INVENTARIOS_CLIENTES_DESPACHOS.md`.

---

## 2. Conexión persistente a la Base de Datos

| Elemento | Implementación | Referencia |
|---|---|---|
| Cadena de conexión | `Server=localhost;Database=LogiTrackDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` | `POS.Server/appsettings.json:9-11` |
| Registro de EF Core | `AddDbContext<AppDbContext>(options => options.UseSqlServer(...GetConnectionString("DefaultConnection")))` | `POS.Server/Program.cs:16-17` |
| Contexto | `public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)` | `POS.Server/Data/AppDbContext.cs:6` |
| Entidades mapeadas | 18 tablas + 2 vistas (`TrazabilidadPublica`, `ReporteEnvios`) | `POS.Server/Data/AppDbContext.cs:9-36` |
| Esquema y datos | Script idempotente `database.sql`: 18 tablas, 2 vistas, 23 triggers | `database.sql` |
| Hosted | `MapStaticAssets()` + `MapFallbackToFile("index.html")` + `MapControllers()` en el mismo puerto | `POS.Server/Program.cs:65-74` |
| Middleware de errores | `ErrorHandlingMiddleware` traduce `BusinessException` y errores SQL a `ApiError` | `POS.Server/Program.cs:58` |

No se usa almacenamiento en memoria: cada controlador inyecta `AppDbContext` (`private readonly AppDbContext _context`) y persiste con `await _context.SaveChangesAsync()`.

---

## 3. Formularios implementados

Los formularios son componentes `MudDialog` invocados desde las páginas. Las bajas usan `DialogService.ShowMessageBoxAsync` con botones **Sí / No**.

| Formulario | Invocado desde | Campos principales | Validaciones | Escritura |
|---|---|---|---|---|
| `ClienteDialog` | `Clientes.razor:94,111` | Razón social, NIT, teléfono, dirección | `Required`, `MaxLength`; razón social y NIT no vacíos (`:11-17`, `:60-64`) | Crear/Guardar `:21-27`; baja desde `Clientes.razor:122-134` → `DELETE /api/clientes/{id}` |
| `ProductoDialog` | `Flota.razor:222,239` | SKU, nombre, peso kg, volumen m³ | `Required`, `MaxLength`, `Min`; peso y volumen > 0 (`:11-23`, `:67-70`) | Crear/Guardar `:28-34`; baja `Flota.razor:250-262` → `DELETE /api/productos/{id}` |
| `VehiculoDialog` | `Flota.razor:167,184` | Placa, tipo, capacidad kg, capacidad m³, estado | `Required`, `MaxLength`, `Min`; estado bloqueado si “En ruta” (`:11-30`, `:90-94`) | Crear/Guardar `:42-48`; `Delete` de UI → `PATCH /api/vehiculos/{id}/estado` (`Flota.razor:195-209`) |
| `GuiaDialog` | `Guias.razor:214,238`; `GuiaDetalle.razor:329` | Cliente, almacén, tipo de carga, hoja opcional, destinatario, dirección, peso, volumen, detalle de productos | Cliente/almacén/tipo, destinatario/dirección, ≥1 producto, sin duplicados, peso/volumen > 0 (`:235-289`) | Registrar/Guardar `:122-128`; quitar línea `:110-112` con confirmación `:220-223` |
| `AsignarHojaDialog` | `Guias.razor:253`; `GuiaDetalle.razor:343` | Guía, selector de hoja clearable | Solo hojas no finalizadas (`:68-71`); vacío desasigna (`:84-99`) | Guardar `:42-48` → `PATCH /api/guias/{id}/hoja-ruta` |
| `MovimientoDialog` | `Inventario.razor:167` | Almacén, tipo `ENTRADA`/`SALIDA`, producto, cantidad, guía opcional, stock disponible | Almacén/producto/cantidad > 0; `SALIDA ≤ stock` RN14 (`:129-141`) | Registrar movimiento `:67-73` → `POST /api/inventario/movimientos` |
| `CheckpointDialog` | `GuiaDetalle.razor:357`; `HojaRutaDetalle.razor:147` | Guía, hito actual, siguiente hito, almacén-estación opcional | Sin hito siguiente o hito final → solo aviso; “Entregado” deriva a entrega (`:24-35`, `:103-108`) | Registrar checkpoint `:55-62` → `POST /api/checkpoints` |
| `IncidenciaDialog` | `GuiaDetalle.razor:371` | Tipo, descripción, foto | Descripción máx. 500; foto PNG/JPG/WEBP ≤5 MB RN08 (`:112-124`, `:144-160`) | Registrar incidencia `:58-65` → `POST /api/incidencias` |
| `EntregaDialog` | `GuiaDetalle.razor:385` | Receptor, firma, foto | Firma o foto RN07; PNG/JPG/WEBP ≤5 MB (`:96-100`, `:131-135`) | Confirmar entrega `:59-67` → `POST /api/entregas` |
| `HojaRutaDialog` | `HojasRuta.razor:117` | Conductor/Operador, vehículo | Ambos `Required`; recursos activos/disponibles RN04 (`:93-97`) | Crear hoja `:49-56` → `POST /api/hojas-ruta` |
| `UsuarioDialog` | `Usuarios.razor:118,135` | Nombre, correo, rol, contraseña inicial (alta), activo | `Required`, email, máx. 120; contraseña solo en alta (`:19-41`, `:108-118`) | Crear/Guardar `:46-53` → `POST/PUT /api/usuarios` |
| `ResetPasswordDialog` | `Usuarios.razor:147` | Nueva contraseña, confirmación | Longitud ≥8 e igualdad (`:55-65`) | Restablecer `:25-31` → `POST /api/usuarios/{id}/reset-password` |

Botones de entrada confirmados: `Nuevo cliente` (`Clientes.razor:17-18`), `Nuevo producto` (`Flota.razor:72-73`), `Nuevo vehículo` (`Flota.razor:27-28`), `Nueva guía` (`Guias.razor:19-20`), `Registrar movimiento` (`Inventario.razor:20-21`) y `Delete` (`Clientes.razor:49-50`, `Flota.razor:55-57`, `Flota.razor:93-94`, `Guias.razor:112-113`).

Roles UI (línea 2 de cada página): Clientes/Flota = AS + AL; Guías = AS + AL + CC; Inventario = AS + AL + PA; Usuarios = AS. Acciones de edición en guías están dentro de `AuthorizeView Roles="@Roles.AdministradorLogistico"` (`Guias.razor:18-21,105-115`).

---

## 4. Controladores CRUD

Leyenda de roles (`POS.Shared/Constants/Roles.cs:7-13`): **AS** Administrador del Sistema, **AL** Administrador Logístico, **C** Conductor/Operador, **PA** Personal de Almacén, **CC** Centro de Control/Supervisor. **Auth** = cualquier usuario autenticado.

| Entidad | Endpoints | Roles | Validaciones / reglas | Persistencia |
|---|---|---|---|---|
| Clientes | `GET/POST/PUT` `ClientesController.cs:24,48,71`; `DELETE :92` | Auth / AS+AL | 404 si no existe (`:80,96`); 409 si tiene guías (`:99-102`) | `Save` `:66,88,105` |
| Productos | `GET/POST/PUT` `ProductosController.cs:24,45,68`; `DELETE :89` | Auth / AS+AL | SKU upper `:56`; 404 `:77,93`; 409 si hay stock, movimientos o detalles (`:96-101`) | `Save` `:63,85,104` |
| Guías | `GET/POST/PUT/PATCH/DELETE` `GuiasController.cs:24,212,309,384,407` | Auth con ámbito / AL | Detalle ≥1, sin duplicados, productos existen, almacén existe, stock suficiente en origen, hito `Registrada`; 400/404/409 (`:216-275,318-352,393-396,417-429`) | `Save` `:299,375,399`; **Tx** `:355-371`, `:431-438` |
| Inventario | `GET/POST` `InventarioController.cs:24,57,77,134` | Auth / PA+AL+AS | Tipo `ENTRADA`/`SALIDA`; 404 almacén/producto/guía (`:143-161`); stock lo valida el trigger RN14 | `Save` `:174` |
| Checkpoints | `GET/POST` `CheckpointsController.cs:24,35` | Auth con ámbito / C+PA | Hito siguiente RN05, guía asignada, hoja activa, conductor asignado RN13, stock suficiente (`:44-101`) | **Tx** `:112-133` |
| Incidencias | `POST IncidenciasController.cs:70` | C+CC | Foto obligatoria RN08 (validación UI y API) | `Save` |
| Entregas | `POST EntregasController.cs:67` | C | Firma o foto RN07; hito `En Reparto Final` | `Save` |
| Hojas de ruta | `GET/POST` `HojasRutaController.cs:24,236` | Auth / AL | Vehículo y conductor activos, RN04 (`:245-272`) | `Save` `:283` |
| Vehículos | `GET/POST/PUT/PATCH` `VehiculosController.cs:32,66,92,116` | Auth / AS+AL | Estados válidos; 404; trigger `trg_vehiculo_estado_valida` (`database.sql:994`) | `Save` `:87,112,126` |
| Usuarios | `GET/POST/PUT/PATCH/POST reset` `UsuariosController.cs:23,69,100,128,149` | AS | Correo duplicado 409; no auto-inhabilitar; BCrypt (`:80-83,114-117,134-137,160`) | `Save` `:95,124,140,161` |
| Notificaciones | `GET/PATCH` `NotificacionesController.cs:22,52,70` | Auth | Solo notificaciones del usuario; 404 si no es propia (`:56-58`) | `Save` `:61,83` |

---

## 5. Lógica de transacciones

### 5.1 Bloques explícitos

Solo hay tres bloques con transacción explícita; en todos se usa `await using`, por lo que una excepción antes del `CommitAsync()` revierte automáticamente sin `RollbackAsync()` manual.

| Operación | Inicio | Saves | Commit | Objetivo |
|---|---|---|---|---|
| Editar guía y reemplazar detalle | `GuiasController.cs:355` | `:358,370` | `:371` | Evitar guía con detalle viejo + nuevo a medias |
| Eliminar guía | `GuiasController.cs:431` | `:434,437` | `:438` | Borrar `DetalleGuia` y `GuiaEnvio` juntos |
| Checkpoint de salida | `CheckpointsController.cs:112` | `:132` | `:133` | Checkpoint + movimientos `SALIDA` + descuento de stock atómicos |

El resto de mutaciones usan una sola llamada `SaveChangesAsync()`, que EF Core ejecuta dentro de su propia transacción implícita.

### 5.2 Descuento automático de mercancía

Al registrar `Salida de Almacén` (`Hitos.SalidaAlmacen`):

1. `CheckpointsController.cs:85-101` valida stock por cada producto del detalle en `guia.IdAlmacenOrigen`.
2. `CheckpointsController.cs:116-130` agrega un `MovimientoInventario` por línea con `TipoMovimiento = SALIDA`, `IdGuia`, `IdAlmacenOrigen`, `IdProducto`, `Cantidad` e `IdUsuario`.
3. El trigger `trg_movimiento_inventario` (`database.sql:1018-1079`) descuenta `stock_actual`; si quedaría negativo ejecuta `ROLLBACK TRANSACTION` y `THROW 50016` (RN14).
4. `CommitAsync()` confirma todo junto. Si falla, no quedan checkpoint ni movimientos.

### 5.3 Triggers que participan

| Trigger | Línea en `database.sql` | Efecto |
|---|---|---|
| `trg_hoja_ruta_notifica` | 461-475 | `Nueva ruta asignada` con `id_hoja_ruta` |
| `trg_guia_envio_asigna_notifica` | 654-672 | `Nueva guía asignada` al asignar guía |
| `trg_checkpoint_valida` | 678-750 | Valida hito siguiente, actualiza hito/hoja/vehículo |
| `trg_incidencia_notifica` | 785-801 | Notifica incidencia |
| `trg_entrega_notifica` | 885-899 | Notifica entrega finalizada |
| `trg_movimiento_inmutable` | 952-957 | Prohíbe modificar/borrar movimientos |
| `trg_detalle_guia_bloqueo` | 961-981 | Bloquea detalle de guía despachada/entregada |
| `trg_vehiculo_no_delete` / `trg_usuario_no_delete` | 985 / 340 | Baja lógica, no borrado físico |
| `trg_movimiento_inventario` | 1018-1079 | Aplica stock y rollback por RN14 |

---

## 6. Trazabilidad formulario → controlador → transacción → BD

| Flujo | Formulario / botón | Endpoint | Transacción | Efecto en BD |
|---|---|---|---|---|
| Alta de cliente | `ClienteDialog` | `POST /api/clientes` | Implícita | `INSERT dbo.cliente` |
| Alta de producto | `ProductoDialog` | `POST /api/productos` | Implícita | `INSERT dbo.producto` |
| Movimiento manual | `MovimientoDialog` | `POST /api/inventario/movimientos` | Implícita | `INSERT movimiento_inventario` + trigger ajusta `inventario.stock_actual` |
| Alta de guía | `GuiaDialog` | `POST /api/guias` | Implícita | Valida stock en origen; `INSERT guia_envio` + `detalle_guia`; notifica si nace asignada |
| Asignar hoja | `AsignarHojaDialog` | `PATCH /api/guias/{id}/hoja-ruta` | Implícita | Actualiza `id_hoja_ruta` + trigger de notificación |
| Editar detalle de guía | `GuiaDialog` (edición) | `PUT /api/guias/{id}` | **Explícita** `GuiasController.cs:355-371` | `DELETE/INSERT detalle_guia` atómico |
| Salida de camión | `CheckpointDialog` | `POST /api/checkpoints` | **Explícita** `CheckpointsController.cs:112-133` | `INSERT checkpoint` + `movimientos SALIDA` + descuento atómico |
| Baja de guía | `Guias.razor Delete` | `DELETE /api/guias/{id}` | **Explícita** `GuiasController.cs:431-438` | `DELETE detalle_guia` + `guia_envio` atómico |
| Inhabilitar vehículo | `Flota.razor Delete` | `PATCH /api/vehiculos/{id}/estado` | Implícita | Update de estado validado por trigger |

---

## 7. Evidencia verificada

### 7.1 Compilación

```powershell
dotnet build POS.Server\POS.Server.csproj --nologo -p:OutputPath=C:\Users\humbe\AppData\Local\Temp\opencode\logitrack-server\
```

Resultado: **0 advertencias, 0 errores**.

### 7.2 Persistencia y esquema

```powershell
sqlcmd -S localhost -E -d LogiTrackDB -W -s "|" -Q "SELECT COUNT(*) FROM sys.tables; SELECT COUNT(*) FROM sys.views; SELECT COUNT(*) FROM sys.triggers WHERE parent_class=1;"
```

Resultado documentado en `README_LogiTrack.md:710`: **18 tablas, 2 vistas, 23 triggers**.

### 7.3 Transacción de salida real

- Guía `LT-00000009`, producto `SKU-0004`, cantidad `2`, almacén `1`.
- Checkpoint `Salida de Almacén` → `idCheckpoint = 16`.
- Stock `109 → 107`; movimiento `id=10`, `tipo=SALIDA`, `id_guia=9`.

### 7.4 Rollback por stock insuficiente

Producto `SKU-0005`, cantidad `999`, stock `50` → `400`:

```json
{
  "codigo": 400,
  "mensaje": "Stock insuficiente en el almacén de origen para el producto 5: disponible 50, requerido 999.",
  "detalle": null,
  "traza": null
}
```

Verificación: stock sin cambios, **0 checkpoints** y **0 movimientos** para esa guía.

### 7.5 Colección de pruebas

`Postman_LogiTrack.json`: JSON válido, **17 carpetas / 68 peticiones** (`README_LogiTrack.md:534`).

### 7.6 Demostración CRUD en Postman

Guía paso a paso con peticiones y respuestas reales capturadas: `EVIDENCIA_POSTMAN_CRUD.md`.

---

## 8. Reproducción

```powershell
cd D:\Projects\Logitrack-DSII
Get-Process -Name POS.Server -ErrorAction SilentlyContinue | Stop-Process -Force
sqlcmd -S localhost -E -b -f 65001 -i database.sql
dotnet run --project POS.Server --launch-profile http
```

Abrir `http://localhost:5163` y validar:

1. **Ana** crea cliente, producto y una `ENTRADA` desde los formularios.
2. **Ana** crea guía y la asigna a la Hoja 1 desde `AsignarHojaDialog`.
3. **Carlos** registra `Salida de Almacén` desde `CheckpointDialog`.
4. Verificar stock descontado y movimiento vinculado a la guía en **Existencias / Movimientos**.
5. Forzar una salida mayor al stock y confirmar el `400` con rollback.
6. Probar bajas: cliente/producto con relaciones → `409`; guía `Registrada` sin eventos → se elimina; vehículo → `Inhabilitado`.

---

## 9. Conclusión

- **Formularios**: 12 diálogos MudBlazor cubren altas, ediciones, asignaciones, eventos de despacho, usuarios y bajas confirmadas.
- **Controladores**: exponen CRUD con autenticación JWT, roles, 404/409 y reglas RN03-RN14.
- **Transacciones**: tres bloques explícitos y `SaveChangesAsync()` atómico; triggers de SQL Server garantizan stock, inmutabilidad y baja lógica.
- **Persistencia**: `AppDbContext` → SQL Server `LogiTrackDB`, verificable después de reiniciar la aplicación.
