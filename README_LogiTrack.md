# LogiTrack Solutions — Sistema de Gestión y Trazabilidad Logística

Aplicación web desarrollada para la materia **Desarrollo de Sistemas II (UPDS)**. Implementa los requisitos funcionales y no funcionales descritos en `Contexto.docx` usando **Blazor WebAssembly + ASP.NET Core Web API + SQL Server**.

La solución incluye:

- Interfaz web en español con identidad visual de **LogiTrack Solutions**.
- API REST protegida con JWT y autorización por roles.
- Base de datos `LogiTrackDB` con reglas de negocio implementadas mediante triggers, constraints, columnas calculadas y vistas.
- Datos de demostración y siete usuarios de prueba.
- Colección Postman con 68 peticiones para probar todos los módulos.

---

## 1. Arquitectura

Modelo **hosted**: el proyecto `POS.Server` sirve los archivos estáticos de `POS.Client`, por lo que la aplicación y la API comparten el mismo origen (`http://localhost:5163`). Esto evita configurar CORS y URLs absolutas en el cliente.

```
Navegador
   │
   ├── Blazor WebAssembly (POS.Client)  ── HTTP/JWT ──►  ASP.NET Core API (POS.Server)
   │                                                        │
   │                                                        └──► SQL Server (LogiTrackDB)
   │                                                                  ├── 18 tablas
   │                                                                  ├── 2 vistas
   │                                                                  └── 23 triggers
   │
   └── Archivos estáticos servidos por POS.Server
```

### Tecnologías

| Componente | Tecnología |
|---|---|
| Frontend | Blazor WebAssembly, .NET 9, MudBlazor 9.12.0 |
| Backend | ASP.NET Core Web API, .NET 9 |
| Base de datos | Microsoft SQL Server 2019 o superior |
| Autenticación | JWT Bearer + BCrypt para contraseñas |
| Acceso a datos | Entity Framework Core (solo lectura/escritura, sin migraciones) |
| Pruebas | Postman v2.1, xUnit, Playwright |

---

## 2. Estructura del repositorio

```
POS/
├── Contexto.docx              # Requisitos del proyecto
├── database.sql               # Script que crea y recrea LogiTrackDB
├── logo.png                   # Identidad visual
├── Postman_LogiTrack.json     # Colección Postman (68 peticiones)
├── README_LogiTrack.md        # Este documento
└── Plantilla/
    ├── POS.slnx               # Solución
    ├── POS.Server/            # API + host de Blazor
    │   ├── Controllers/       # 16 controladores REST
    │   ├── Data/              # AppDbContext y mapeo EF Core
    │   ├── Infrastructure/    # Errores, JWT, traducción de errores SQL
    │   ├── Services/          # AuthService
    │   ├── wwwroot/uploads/   # Firmas, fotos y evidencias
    │   └── Program.cs
    ├── POS.Client/            # Blazor WASM
    │   ├── Pages/             # 19 páginas
    │   ├── Dialogs/           # 12 diálogos de alta/edición
    │   ├── Layout/             # MainLayout, NavMenu, EmptyLayout
    │   ├── Services/          # ApiClient, JWT, catálogos, localStorage
    │   └── wwwroot/           # CSS, JS, logo, manifest
    └── POS.Shared/            # Entidades, DTOs, constantes y enums
```

---

## 3. Requisitos previos

1. **SQL Server 2019 o superior** (Express, Developer o Standard), con autenticación de Windows habilitada.
2. **.NET SDK 9.0** o superior. Verificar con:

   ```powershell
   dotnet --version
   ```

3. **sqlcmd** incluido con SQL Server o las herramientas de línea de comandos.
4. **Postman** (opcional, para ejecutar la colección de pruebas).
5. Navegador moderno (Chrome, Edge o Firefox).

> El proyecto apunta a `net9.0` para que el Explorador de pruebas funcione con Visual Studio 17.x. Para apuntar a `net10.0` se requiere Visual Studio 18.0 o superior.

---

## 4. Configuración de la base de datos

### 4.1 Cargar el esquema

Desde la raíz del repositorio:

```powershell
sqlcmd -S localhost -E -b -f 65001 -i database.sql
```

- `-S localhost`: instancia local de SQL Server. Cambiar por `localhost\SQLEXPRESS` si corresponde.
- `-E`: autenticación de Windows.
- `-b`: detener la ejecución si hay errores.
- `-f 65001`: codificación UTF-8 para conservar tildes y ñ.

Si se usa autenticación SQL:

```powershell
sqlcmd -S localhost -U sa -P "TuClave" -b -f 65001 -i database.sql
```

> **Importante:** `database.sql` **elimina y recrea** `LogiTrackDB`. Antes de volver a ejecutarlo, detener la aplicación (`Ctrl+C` en la terminal donde corre `dotnet run`) y cerrar sesiones abiertas en SSMS, porque el script usa `SINGLE_USER WITH ROLLBACK IMMEDIATE`.

### 4.2 Verificar la carga

```powershell
sqlcmd -S localhost -E -d LogiTrackDB -Q "SELECT COUNT(*) AS tablas FROM sys.tables; SELECT COUNT(*) AS triggers FROM sys.triggers; SELECT COUNT(*) AS usuarios FROM dbo.usuario;"
```

Resultado esperado:

| Métrica | Valor |
|---|---|
| Tablas | 18 |
| Triggers | 23 |
| Usuarios demo | 7 |
| Guías demo | 4 |
| Hojas de ruta demo | 2 |

### 4.3 Cadena de conexión

Archivo: `Plantilla/POS.Server/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LogiTrackDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "LogiTrackSolutions-UPDS-2026-Clave-JWT-Segura-1234567890",
    "Issuer": "LogiTrack.Server",
    "Audience": "LogiTrack.Client",
    "ExpiresMinutes": 480
  }
}
```

Si SQL Server usa otra instancia, usuario o contraseña, ajustar únicamente `DefaultConnection`.

---

## 5. Compilación y ejecución

### 5.1 Restaurar y compilar

```powershell
cd "D:\UPDS\Desarrollo de Sistemas II\POS\Plantilla"
dotnet restore POS.slnx
dotnet build POS.slnx
```

Resultado esperado: **0 errores y 0 advertencias**.

### 5.2 Ejecutar la aplicación

```powershell
dotnet run --project POS.Server --launch-profile http
```

Abrir en el navegador:

```
http://localhost:5163
```

El servidor sirve la interfaz Blazor y la API en el mismo puerto. La API responde en `http://localhost:5163/api/...`.

Para usar HTTPS (opcional):

```powershell
dotnet run --project POS.Server --launch-profile https
```

Luego abrir `https://localhost:7225`. Si el navegador advierte por el certificado de desarrollo, ejecutar una vez:

```powershell
dotnet dev-certs https --trust
```

### 5.3 Publicar en Release (opcional)

```powershell
dotnet publish POS.Server\POS.Server.csproj -c Release -o C:\logitrack-publicado
```

Para ejecutar la versión publicada:

```powershell
$env:ASPNETCORE_URLS = "http://localhost:5170"
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet C:\logitrack-publicado\POS.Server.dll
```

Abrir `http://localhost:5170`. La publicación incluye la interfaz, los assets de MudBlazor y la API; usa el mismo `appsettings.json` y la misma base `LogiTrackDB`.

### 5.4 Detener la aplicación

`Ctrl+C` en la terminal de ejecución. Si el proceso queda en segundo plano:

```powershell
Get-Process -Name POS.Server -ErrorAction SilentlyContinue | Stop-Process -Force
```

---

## 6. Credenciales de demostración

Todos los usuarios comparten la contraseña:

```
LogiTrack2026!
```

| Usuario | Correo | Rol | Uso principal |
|---|---|---|---|
| Ana Rojas | `ana.rojas@logitrack.test` | Administrador Logístico | Guías, hojas de ruta, flota, clientes, productos |
| Luis Mendez | `luis.mendez@logitrack.test` | Administrador del Sistema | Usuarios, configuración, reportes |
| Carlos Soliz | `carlos.soliz@logitrack.test` | Conductor/Operador | Checkpoints, incidencias, entregas, sus rutas |
| Marta Vaca | `marta.vaca@logitrack.test` | Personal de Almacén | Inventario y movimientos |
| Pedro Egüez | `pedro.eguez@logitrack.test` | Centro de Control/Supervisor | Consulta, incidencias y reportes |
| Diego Vargas | `diego.vargas@logitrack.test` | Conductor/Operador | Segunda hoja de ruta demo |
| Sofía Campos | `sofia.campos@logitrack.test` | Conductor/Operador (inactiva) | Prueba de bloqueo de usuarios inactivos |

> Sofía está **inactiva** a propósito: el login debe rechazarla. No se elimina ningún usuario (RN11).

### Estado inicial de los datos demo

| Guía | Estado inicial | Hoja de ruta |
|---|---|---|
| `LT-00000001` | Entregado | 1 — Carlos |
| `LT-00000002` | En Tránsito | 1 — Carlos |
| `LT-00000003` | Salida de Almacén | 2 — Diego |
| `LT-00000004` | Registrada | Sin asignar |

La guía `LT-00000004` y la hoja de ruta 1 son las más útiles para probar el flujo completo.

---

## 7. Funcionalidades por rol

| Módulo | Administrador del Sistema | Administrador Logístico | Conductor/Operador | Personal de Almacén | Supervisor |
|---|:---:|:---:|:---:|:---:|:---:|
| Dashboard / Inicio | ✅ | ✅ | ✅ | ✅ | ✅ |
| Rastreo público | ✅ | ✅ | ✅ | ✅ | ✅ |
| Guías: consultar | ✅ | ✅ | Limitado a sus rutas | ✅ | ✅ |
| Guías: crear/editar/asignar | — | ✅ | — | — | — |
| Hojas de ruta: consultar | ✅ | ✅ | Solo las suyas | — | ✅ |
| Hojas de ruta: crear | — | ✅ | — | — | — |
| Checkpoints | — | — | ✅ | ✅ | — |
| Incidencias: registrar | — | — | ✅ | — | ✅ |
| Entregas: registrar | — | — | ✅ | — | — |
| Inventario / movimientos | ✅ | ✅ | — | ✅ | — |
| Notificaciones | ✅ | ✅ | ✅ | ✅ | ✅ |
| Reportes | ✅ | ✅ | — | — | ✅ |
| Usuarios | ✅ | — | — | — | — |
| Clientes / productos / vehículos | ✅ | ✅ | — | — | — |
| Archivos (firmas, fotos) | ✅ | ✅ | ✅ | ✅ | ✅ |

La navegación lateral se genera dinámicamente según el rol del JWT.

---

## 8. Reglas de negocio implementadas en la base de datos

Las reglas se validan en SQL Server; la API traduce los errores a mensajes claros con el contrato `ApiError { Codigo, Mensaje, Detalle, Traza }`.

| Regla | Implementación |
|---|---|
| RN01 | Código de guía único e inmutable: columna calculada `LT-` + 8 dígitos + restricción única |
| RN02 | Datos mínimos obligatorios mediante columnas `NOT NULL` y `CHECK` |
| RN03 | Una guía solo se asigna una vez, desde `Registrada` y a una hoja no `Finalizada` |
| RN04 | Conductor y vehículo habilitados, sin hoja activa duplicada |
| RN05 | Avance ordenado de hitos mediante trigger de checkpoint |
| RN06 | Checkpoint válido: guía con hoja de ruta y usuario autorizado |
| RN07 | Entrega con firma y foto obligatorias; marca `Entregado` y bloquea cambios |
| RN08 | Incidencia con descripción y foto obligatorias |
| RN09 | Notificaciones internas generadas por triggers (`Nueva ruta asignada`, `Nueva guía asignada`, incidencia y entrega) |
| RN10 | Rastreo público sin datos personales (vista dedicada) |
| RN11 | Los usuarios no se eliminan; solo se activan/desactivan |
| RN12 | Separación de funciones por rol a nivel de API y de base de datos |
| RN13 | El conductor solo opera sobre guías de sus propias hojas de ruta |
| RN14 | Integridad de inventario y bloqueo de stock negativo |
| RNF05 | Auditoría temporal y consistencia transaccional |

### 8.1 Transacciones en el control de mercancía

- Cada `SaveChangesAsync()` se ejecuta dentro de una transacción de EF Core, por lo que el movimiento y el ajuste de stock que hace el trigger `trg_movimiento_inventario` (`database.sql:1018-1079`) quedan atómicos: si el stock es insuficiente, el trigger ejecuta `ROLLBACK TRANSACTION` y `THROW 50016` (RN14), sin dejar movimientos huérfanos.
- Al registrar el checkpoint **Salida de Almacén** (`CheckpointsController.cs:85-133`) se generan automáticamente los movimientos `SALIDA` del almacén de origen por cada producto del detalle de la guía. El checkpoint, los movimientos y el descuento de stock se confirman juntos con `CommitAsync()`; si falta stock, no se registra ni el checkpoint. No registrar además un movimiento manual `SALIDA` para esa guía, porque duplicaría el descuento.
- Los trigger de hito, notificaciones y eventos también se ejecutan en la misma transacción de la petición. `trg_guia_envio_asigna_notifica` avisa al conductor cuando su guía pasa de `Sin asignar` a una hoja de ruta (`database.sql:651-670`).
- Cuando una operación escribe en varias tablas con más de un `SaveChangesAsync()`, se usa una transacción explícita:
  - `GuiasController.cs:355-371`: reemplazo del detalle de la guía (RN05/RNF05).
  - `GuiasController.cs:431-438`: baja de la guía; primero elimina `detalle_guia` y luego la guía, con `CommitAsync()` al final.
- Las bajas de cliente y producto validan las FK antes de borrar y devuelven `409` si el registro está referenciado, evitando estados parciales.

---

## 9. API REST

Base URL local:

```
http://localhost:5163
```

Autenticación: enviar el token en la cabecera:

```
Authorization: Bearer <token>
```

Contrato de error:

```json
{
  "codigo": 50011,
  "mensaje": "RN03: una guía despachada no puede quitarse de su hoja de ruta.",
  "detalle": null,
  "traza": null
}
```

### 9.1 Autenticación

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Público | Inicia sesión y devuelve el JWT |
| GET | `/api/auth/me` | Autenticado | Datos del usuario del token |
| POST | `/api/auth/cambiar-password` | Autenticado | Cambia la contraseña propia |

### 9.2 Catálogos

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/catalogos` | Autenticado | Roles, almacenes, tipos, hitos, clientes, productos, vehículos y usuarios por rol |
| GET | `/api/catalogos/conductores` | Autenticado | Conductores activos |

### 9.3 Guías de envío

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/guias?page=1&pageSize=20&buscar=&idHito=&idCliente=&idHojaRuta=&desde=&hasta=` | Autenticado | Listado paginado |
| GET | `/api/guias/{id}` | Autenticado | Detalle con detalles e indicadores |
| GET | `/api/guias/{id}/checkpoints` | Autenticado | Historial de hitos |
| GET | `/api/guias/{id}/incidencias` | Autenticado | Incidencias de la guía |
| GET | `/api/guias/{id}/entrega` | Autenticado | Comprobante de entrega |
| GET | `/api/guias/{id}/siguiente-hito` | Autenticado | Siguiente hito permitido (204 si solo queda `Entregado`) |
| POST | `/api/guias` | Administrador Logístico | Crea la guía y sus detalles |
| PUT | `/api/guias/{id}` | Administrador Logístico | Actualiza una guía `Registrada` |
| PATCH | `/api/guias/{id}/hoja-ruta` | Administrador Logístico | Asigna o quita la hoja de ruta |
| DELETE | `/api/guias/{id}` | Administrador Logístico | Baja de una guía `Registrada` sin eventos de trazabilidad (RNF05) |

### 9.4 Hojas de ruta

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/hojas-ruta` | Autenticado | Listado con guías y totales |
| GET | `/api/hojas-ruta/{id}` | Autenticado | Detalle |
| GET | `/api/hojas-ruta/mias` | Conductor | Hojas no finalizadas del conductor |
| GET | `/api/hojas-ruta/recursos` | Administrador Logístico | Conductores y vehículos libres |
| POST | `/api/hojas-ruta` | Administrador Logístico | Crea una hoja de ruta |

### 9.5 Checkpoints

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/checkpoints/guia/{idGuia}` | Autenticado | Checkpoints de una guía |
| POST | `/api/checkpoints` | Conductor / Personal de Almacén | Registra el avance de hito |

Cuerpo:

```json
{
  "idGuia": 4,
  "idHito": 2,
  "idAlmacen": 1
}
```

### 9.6 Incidencias

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/incidencias?page=1&pageSize=20&idGuia=&idTipoIncidencia=` | Autenticado | Listado paginado |
| GET | `/api/incidencias/guia/{idGuia}` | Autenticado | Incidencias de una guía |
| POST | `/api/incidencias` | Conductor / Supervisor | Registra una incidencia |

Cuerpo:

```json
{
  "idGuia": 4,
  "idTipoIncidencia": 1,
  "descripcion": "Cierre vial reportado durante la ruta.",
  "rutaFoto": "/uploads/evidencias/foto.png"
}
```

### 9.7 Entregas

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/entregas?page=1&pageSize=20&idGuia=` | Autenticado | Listado paginado |
| GET | `/api/entregas/guia/{idGuia}` | Autenticado | Entrega de una guía |
| POST | `/api/entregas` | Conductor | Registra la entrega con firma y foto |

Cuerpo:

```json
{
  "idGuia": 4,
  "nombreReceptor": "María Pérez",
  "rutaFirma": "/uploads/firmas/firma.png",
  "rutaFoto": "/uploads/fotos/entrega.png"
}
```

La entrega marca la guía como `Entregado` y finaliza la hoja de ruta si todas sus guías fueron entregadas.

### 9.8 Inventario

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/inventario?idAlmacen=&buscar=` | Autenticado | Stock por almacén y producto |
| GET | `/api/inventario/alertas?umbral=20` | Autenticado | Productos con stock bajo |
| GET | `/api/inventario/movimientos?idAlmacen=&idProducto=&tipo=&desde=&hasta=&page=1&pageSize=20` | Autenticado | Movimientos paginados |
| POST | `/api/inventario/movimientos` | Almacén / Admin. Logístico / Admin. Sistema | Registra `ENTRADA` o `SALIDA` |

Cuerpo:

```json
{
  "idAlmacen": 1,
  "idProducto": 1,
  "idGuia": null,
  "tipoMovimiento": "ENTRADA",
  "cantidad": 10
}
```

### 9.9 Notificaciones

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/notificaciones?leida=false&limite=20` | Autenticado | Notificaciones del usuario |
| GET | `/api/notificaciones/no-leidas` | Autenticado | Contador de no leídas |
| PATCH | `/api/notificaciones/{id}/leida` | Autenticado | Marca una notificación como leída |
| PATCH | `/api/notificaciones/leer-todas` | Autenticado | Marca todas como leídas |

### 9.10 Reportes

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/reportes/dashboard` | Autenticado | Indicadores del panel |
| GET | `/api/reportes/envios?buscar=&estado=&cliente=&desde=&hasta=&conIncidencias=&limite=500` | Admin. Sistema / Admin. Logístico / Supervisor | Reporte de envíos |

### 9.11 Rastreo público

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/tracking/{codigoGuia}` | Público | Estado y eventos sin datos personales |

Ejemplo:

```powershell
Invoke-RestMethod "http://localhost:5163/api/tracking/LT-00000001"
```

### 9.12 Usuarios

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/usuarios` | Administrador del Sistema | Listado |
| GET | `/api/usuarios/{id}` | Administrador del Sistema | Detalle |
| POST | `/api/usuarios` | Administrador del Sistema | Alta |
| PUT | `/api/usuarios/{id}` | Administrador del Sistema | Edición |
| PATCH | `/api/usuarios/{id}/estado` | Administrador del Sistema | Activa/desactiva |
| POST | `/api/usuarios/{id}/reset-password` | Administrador del Sistema | Restablece contraseña |

### 9.12.1 Conductores (CRUD)

CRUD específico del rol `Conductor/Operador`, implementado sobre `dbo.usuario` sin baja física (RN11).

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| GET | `/api/conductores?activo=&buscar=` | Administrador del Sistema | Listado filtrable |
| GET | `/api/conductores/{id}` | Administrador del Sistema | Detalle |
| POST | `/api/conductores` | Administrador del Sistema | Alta; fuerza el rol Conductor/Operador |
| PUT | `/api/conductores/{id}` | Administrador del Sistema | Edición de nombre, correo y estado |
| PATCH | `/api/conductores/{id}/estado` | Administrador del Sistema | Activa/desactiva; no elimina el registro |
| POST | `/api/conductores/{id}/reset-password` | Administrador del Sistema | Restablece la contraseña |

Pantalla Blazor: `/conductores` (menú Administración del Sistema → Conductores), con formulario en línea para alta y edición, filtros de búsqueda/estado y acciones de baja lógica y restablecimiento de contraseña. Peticiones Postman: carpeta `18 Conductores (CRUD)`. Todas requieren `Authorization: Bearer {{tokenAdminSistema}}`.

### 9.13 Clientes, productos y vehículos

| Método | Ruta | Acceso |
|---|---|---|
| GET | `/api/clientes` | Autenticado |
| POST / PUT | `/api/clientes`, `/api/clientes/{id}` | Admin. Sistema / Admin. Logístico |
| DELETE | `/api/clientes/{id}` | Admin. Sistema / Admin. Logístico |
| GET | `/api/productos` | Autenticado |
| POST / PUT | `/api/productos`, `/api/productos/{id}` | Admin. Sistema / Admin. Logístico |
| DELETE | `/api/productos/{id}` | Admin. Sistema / Admin. Logístico |
| GET | `/api/vehiculos`, `/api/vehiculos/{id}` | Autenticado |
| POST / PUT / PATCH | `/api/vehiculos`, `/api/vehiculos/{id}`, `/api/vehiculos/{id}/estado` | Admin. Sistema / Admin. Logístico |

Las bajas son físicas y devuelven `204`. Responden `404` si el registro no existe y `409` si está referenciado: un cliente con guías de envío; un producto con stock, movimientos o detalles de guía; una guía que no esté `Registrada` o que ya tenga checkpoints, incidencias, entregas, movimientos o notificaciones (RNF05).

### 9.14 Archivos

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| POST | `/api/files/upload` | Autenticado | Sube firma, foto o evidencia (multipart/form-data) |

Campos del formulario:

| Campo | Tipo | Descripción |
|---|---|---|
| `archivo` | File | PNG, JPG o WEBP, máximo 5 MB |
| `carpeta` | Text | `firmas`, `fotos` o `evidencias` |

Respuesta:

```json
{
  "ruta": "/uploads/evidencias/49d1d42fbe7c4e0d8e471ea35bc9c829.png",
  "nombre": "evidencia.png"
}
```

---

## 10. Pruebas con Postman

### 10.1 Importar la colección

1. Abrir Postman.
2. `File` → `Import` → seleccionar `Postman_LogiTrack.json`.
3. Verificar que la colección **LogiTrack Solutions - API** tenga 17 carpetas y 68 peticiones.
4. Revisar las variables de colección (`Variables`). La más importante es:

   | Variable | Valor inicial |
   |---|---|
   | `baseUrl` | `http://localhost:5163` |
   | `passwordDemo` | `LogiTrack2026!` |
    | `idGuia` | `4` |
    | `idGuiaEliminar` | `4` |
    | `codigoGuia` | `LT-00000001` |
   | `idHojaRuta` | `1` |
   | `idCliente` | `1` |
   | `idProducto` | `1` |
   | `idVehiculo` | `1` |
   | `idNotificacion` | `1` |

5. Si la API corre en otro puerto, cambiar `baseUrl`.

### 10.2 Orden de ejecución recomendado

Ejecutar las carpetas en este orden:

1. **01 Autenticación** — guarda automáticamente los cinco tokens.
2. **02 Catálogos** — valida catálogos y conductores.
3. **03 Guías de envío** — crear guía y asignarla a la hoja de ruta 1.
4. **05 Checkpoints** — hitos 2, 3, 4 y 5 con el token de Carlos.
5. **06 Incidencias** — registrar una incidencia.
6. **07 Entregas** — registrar la entrega (la guía pasa a `Entregado`).
7. **04 Hojas de ruta** — verificar el estado de la hoja.
8. **08 Inventario** — consultar stock y registrar movimientos.
9. **09 Notificaciones** — listar y marcar como leídas.
10. **10 Reportes** — dashboard y reporte de envíos.
11. **11 Rastreo público** — consultar `{{codigoGuia}}`.
12. **12 a 16** — usuarios, clientes, productos, vehículos y archivos.
13. **17 Bajas (DELETE)** — crear y eliminar una guía aislada; al final de las carpetas 13 y 14 también se eliminan el cliente y el producto creados.

> Las peticiones de escritura guardan los identificadores creados en variables de colección, por lo que las peticiones siguientes usan automáticamente el registro recién creado. La carpeta 17 usa `idGuiaEliminar` para no afectar `idGuia`.

### 10.3 Flujo completo sugerido (smoke test)

1. Ejecutar los cinco **Login** de la carpeta 01.
2. Ejecutar **Crear guía**. Se guardan `idGuia` y `codigoGuia`.
3. Ejecutar **Asignar guía a hoja de ruta** (usa la hoja 1 de Carlos).
4. Ejecutar **Notificaciones** con `tokenConductor`: deben aparecer `Nueva ruta asignada` y `Nueva guía asignada`, cada una con su botón **Ver ruta**.
5. Ejecutar **Checkpoint 1** (Salida de Almacén): verifica en **08 Inventario** que el stock bajó según el detalle de la guía; luego ejecuta los checkpoints **2**, **3** y **4**.
6. Ejecutar **Registrar incidencia**.
7. Ejecutar **Registrar entrega**.
8. Ejecutar **Consultar tracking por código**: debe devolver estado `Entregado` y los eventos del recorrido.
9. Ejecutar **Dashboard** y **Reporte de envíos**.

Si se desea repetir el flujo desde cero, volver a cargar `database.sql` (deteniendo antes el servidor) para restaurar el estado demo.

> La petición **Crear hoja de ruta (opcional)** puede responder `400` o `409` en la carga inicial porque los dos conductores y vehículos del seed ya tienen hoja activa. Es un resultado esperado y la colección lo contempla; no impide el resto del flujo.

### 10.4 Pruebas negativas recomendadas

| Caso | Petición | Resultado esperado |
|---|---|---|
| Login con contraseña incorrecta | `POST /api/auth/login` | `401` |
| Login de Sofía (inactiva) | `POST /api/auth/login` | `401` |
| Crear guía con token de conductor | `POST /api/guias` | `403` |
| Registrar checkpoint fuera de orden | `POST /api/checkpoints` con hito 5 sobre guía `Registrada` | `400` con código de RN05 |
| Conductor opera guía de otra hoja | `POST /api/checkpoints` con token de Diego y guía de Carlos | `403` o error RN13 |
| Entrega sin firma/foto | `POST /api/entregas` sin `rutaFirma` | `400` |
| SALIDA mayor al stock | `POST /api/inventario/movimientos` | `400`/`409` con mensaje de stock |
| Eliminar usuario | No existe endpoint `DELETE` | `405` / no expuesto |
| Eliminar cliente con guías | `DELETE /api/clientes/1` | `409` |
| Eliminar producto con stock | `DELETE /api/productos/1` | `409` |
| Eliminar guía despachada | `DELETE /api/guias/{idGuia}` con hito distinto de `Registrada` | `409` (RNF05) |
| Eliminar con token de Almacén | `DELETE /api/clientes/{id}` o `/api/productos/{id}` | `403` |
| Subir archivo mayor a 5 MB | `POST /api/files/upload` | `400`/`413` |

### 10.5 Subida de archivos en Postman

En la carpeta **16 Archivos**, abrir **Subir archivo** y seleccionar manualmente una imagen en el campo `archivo`. El campo `carpeta` acepta `firmas`, `fotos` o `evidencias`.

La respuesta guarda `rutaArchivo`, que luego se usa en incidencias y entregas.

### 10.6 Ejecución por línea de comandos (opcional)

Si se tiene Newman instalado:

```powershell
npm install -g newman
newman run Postman_LogiTrack.json
```

Para ejecutar solo una carpeta:

```powershell
newman run Postman_LogiTrack.json --folder "01 Autenticación"
```

---

## 11. Pruebas rápidas con PowerShell

### Login

```powershell
$body = @{
    correo   = "ana.rojas@logitrack.test"
    password = "LogiTrack2026!"
} | ConvertTo-Json

$login = Invoke-RestMethod `
    -Uri "http://localhost:5163/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

$token = $login.token
$login.nombreCompleto
```

### Consultar guías

```powershell
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod "http://localhost:5163/api/guias?page=1&pageSize=10" -Headers $headers |
    Select-Object total, page, pageSize
```

### Crear una guía

```powershell
$guia = @{
    idCliente       = 1
    idAlmacenOrigen = 1
    idTipoCarga     = 1
    destinatario    = "Destinatario Demo"
    direccionDestino = "Av. Demo 123"
    pesoKg          = 5.5
    volumenM3       = 0.03
    detalles        = @(@{ idProducto = 1; cantidad = 2 })
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
    -Uri "http://localhost:5163/api/guias" `
    -Method Post `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $guia
```

### Rastreo público

```powershell
Invoke-RestMethod "http://localhost:5163/api/tracking/LT-00000001"
```

---

## 12. Solución de problemas

| Problema | Causa / solución |
|---|---|
| `database.sql` falla con “no se puede obtener acceso exclusivo” | Hay conexiones activas. Detener `POS.Server`, cerrar SSMS y volver a ejecutar. |
| `Login failed for user` | Ajustar `DefaultConnection` a la instancia/usuario correctos. |
| Error `334` al insertar | La tabla tiene triggers y EF Core debe mapearla con `HasTrigger`. Ya está configurado en `AppDbContext`; no quitar esas líneas. |
| `dotnet run` falla por SDK | Instalar .NET 9.0 (Runtime/SDK) o ajustar `TargetFramework` en los archivos `.csproj`. |
| El puerto 5163 está ocupado | Detener el proceso anterior o ejecutar `dotnet run --project POS.Server --urls http://localhost:5170`. |
| La interfaz no carga datos | Verificar que la API esté arriba y que el token no haya expirado (480 minutos). |
| La página queda en blanco o sin estilos de MudBlazor | Ejecutar desde la carpeta `Plantilla` con `dotnet run --project POS.Server --launch-profile http`; no abrir `index.html` como archivo. Si persiste, `dotnet clean POS.slnx` y volver a compilar para regenerar los recursos estáticos. |
| `/_framework/...` o `/_content/MudBlazor/...` responde 404/500 | El servidor debe servir los assets del cliente con `MapStaticAssets()`. No reemplazar por `UseBlazorFrameworkFiles()`/`UseStaticFiles()` ni separar el cliente del servidor al ejecutar. |
| El navegador muestra una versión vieja | `Ctrl+F5` y borrar `localStorage` del sitio. |
| Error 401 después de recargar | El token se guarda en `localStorage`; si se limpió, volver a iniciar sesión. |
| Error al subir archivo | Verificar formato PNG/JPG/WEBP y tamaño máximo de 5 MB. |
| No aparecen conductores en “Recursos” | En la carga inicial ambos conductores ya tienen hoja activa; usar la hoja 1 o finalizar una ruta. |
| El código de guía no se actualiza | `codigo_guia` es una columna calculada; nunca se inserta ni se modifica manualmente. |
| Se ejecutó “Cambiar contraseña” y ya no entra | Recargar `database.sql` para restaurar `LogiTrack2026!`. |

---

## 13. Checklist de entrega

- [x] Base de datos `LogiTrackDB` con 18 tablas, 2 vistas y 23 triggers.
- [x] API REST con 16 controladores y autenticación JWT por roles.
- [x] Interfaz Blazor WASM completa en español.
- [x] Módulos de guías, hojas de ruta, checkpoints, incidencias, entregas, inventario, notificaciones, reportes, usuarios, clientes, flota y rastreo público.
- [x] Carga de firmas, fotos y evidencias.
- [x] Datos demo y siete usuarios de prueba.
- [x] Colección Postman con 68 peticiones.
- [x] Instrucciones de instalación, ejecución y pruebas en este documento.
- [x] [Manual Técnico](docs/entrega_final/01_MANUAL_TECNICO.md).
- [x] [Manual de Usuario](docs/entrega_final/02_MANUAL_DE_USUARIO.md).
- [x] [Reporte Final de Pruebas y Corrección de Errores](docs/entrega_final/03_REPORTE_FINAL_PRUEBAS_Y_CORRECCIONES.md).
- [x] [Guion de Demostración y Defensa Oral](docs/entrega_final/04_GUION_DEMOSTRACION_DEFENSA.md).

---

**LogiTrack Solutions** — Proyecto académico de Desarrollo de Sistemas II, UPDS.
