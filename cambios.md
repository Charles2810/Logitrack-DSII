# LogiTrack Solutions — Cambios implementados (mapas, rutas y simulación)

Fecha: 21/11/2026  
Repositorio: `Logitrack-DSII` (descargado como ZIP desde GitHub; el entorno no tiene `git` instalado).  
Estado: **compila correctamente** (`dotnet build POS.slnx`: 0 errores, 1 advertencia preexistente).  
**No se realizó commit ni push.**

---

## 1. Objetivo

Ampliar el proyecto académico con:

- Mapas interactivos con **Leaflet 1.9.4** (vendored/local, sin CDN para la librería).
- Trazado de rutas viales con **OSRM** (`https://router.project-osrm.org`) y *fallback* automático a ruta estimada cuando el servicio público no responde.
- Formularios con **latitud/longitud** y selección de punto en el mapa.
- **Simulación de entregas**: marcador animado, progreso, distancia restante, ETA y bitácora de hitos simulados.
- Ciudad por defecto: **Santa Cruz de la Sierra**, `-17.783700, -63.180426`, zoom 13.

---

## 2. Base de datos (`database.sql` — único archivo SQL modificado)

Se amplió el script existente; **no se crearon nuevos archivos SQL**:

| Cambio | Detalle |
|---|---|
| `dbo.cliente` | Columnas `latitud`, `longitud` `DECIMAL(9,6)` con `CHECK` de rangos. |
| `dbo.almacen` | Columnas `latitud`, `longitud` `DECIMAL(9,6)` con `CHECK` de rangos. |
| `dbo.guia_envio` | `latitud_destino`, `longitud_destino`, `distancia_estimada_km`, `duracion_estimada_min` con `CHECK`. |
| Índice | `IX_guia_destino_geo` sobre `(latitud_destino, longitud_destino)`. |
| Datos demo | Coordenadas reales de Santa Cruz para 3 almacenes, 3 clientes y 4 guías; distancias 2.40 / 2.70 / 4.30 / 0.80 km y ETA 10 / 12 / 16 / 6 min. |
| Verificación | El script ahora imprime almacenes y guías con sus coordenadas. |

El script se ejecutó con:

```powershell
sqlcmd -S localhost -E -b -f 65001 -i database.sql
```

> El script recrea `LogiTrackDB` (`DROP DATABASE` + `SINGLE_USER WITH ROLLBACK IMMEDIATE`). La BD quedó aplicada y verificada (18 tablas, datos demo cargados).

---

## 3. Capa compartida (`POS.Shared`)

| Archivo | Cambio |
|---|---|
| `Utils/GeoCalculos.cs` **(nuevo)** | Centro Santa Cruz, zoom 13, distancia Haversine, ETA urbano (32 km/h), validación de coordenadas. |
| `DTOs/GuiaDtos.cs` | `CrearGuiaRequest` y `ActualizarGuiaRequest` aceptan `LatitudDestino`/`LongitudDestino`; `GuiaDto` expone origen/destino, distancia, duración y las propiedades calculadas `TieneRuta`, `DistanciaRutaKm`, `DuracionRutaMin`. |
| `DTOs/CatalogoDtos.cs` | `ClienteDto`, `CrearClienteRequest` y `AlmacenDto` incluyen latitud/longitud. |
| `DTOs/ReporteDtos.cs` | `TrazabilidadDto` incluye coordenadas, distancia, ETA y `TieneMapa` (rastreo público sin datos personales). |
| `Entities/Entities.cs` | `Cliente`, `Almacen` y `GuiaEnvio` mapeados a las nuevas columnas. |

---

## 4. Capa servidor (`POS.Server`)

| Archivo | Cambio |
|---|---|
| `Data/AppDbContext.cs` | Precisiones `(9,6)` para coordenadas y `(10,2)` para distancia en el mapeo EF Core. |
| `Infrastructure/GuiaQueries.cs` | Las proyecciones de resumen y detalle incluyen coordenadas del almacén de origen y de destino, distancia y duración. |
| `Controllers/GuiasController.cs` | `POST`/`PUT` guardan coordenadas y calculan distancia/ETA con `GeoCalculos` al crear o editar una guía. |
| `Controllers/ClientesController.cs` | `GET`/`POST`/`PUT` exponen y persisten coordenadas del cliente. |
| `Controllers/CatalogosController.cs` | El catálogo devuelve coordenadas de almacenes y clientes. |
| `Controllers/TrackingController.cs` | El endpoint público `api/tracking/{codigo}` devuelve coordenadas, distancia y ETA para dibujar la ruta sin exponer datos personales. |
| `Controllers/RutasController.cs` **(nuevo)** | `GET api/rutas` y `GET api/rutas/{id}` devuelven guías con coordenadas; respeta el filtro por conductor. |

---

## 5. Capa cliente (`POS.Client`)

### 5.1 Recursos y motor de mapas

| Archivo | Cambio |
|---|---|
| `wwwroot/lib/leaflet/` **(nuevo)** | Leaflet 1.9.4 local: `leaflet.css`, `leaflet.js` y marcadores PNG. |
| `wwwroot/index.html` | Carga de `leaflet.css`, `leaflet.js` y `js/mapa.js`. |
| `wwwroot/css/app.css` | Estilos `.lt-mapa`, marcadores personalizados y ocultación de mapas al imprimir. |
| `wwwroot/js/mapa.js` **(nuevo)** | API `window.logitrackMapas`: inicializar, establecer puntos, trazar con OSRM (fallback estimado), animar/pausar/reanudar/reiniciar, centrar y limpiar. Emite `OnMapaClick`, `OnProgreso` y `OnAnimacionFinalizada` a Blazor. |

### 5.2 Componentes y páginas

| Archivo | Cambio |
|---|---|
| `Components/MapaRutas.razor` **(nuevo)** | Componente reutilizable con `IJSInvokable`, selección de puntos, ruta, animación y eventos. Centro por defecto Santa Cruz, zoom 13. |
| `Components/MapaRutasModels.cs` **(nuevo)** | `MapaRutaDto`, `PuntoMapaDto`, `ProgresoRuta`. |
| `Components/RastreoTracker.razor` | Rastreo público con mapa, distancia/ETA y botón “Simular entrega”. |
| `Dialogs/GuiaDialog.razor` | Campos de latitud/longitud de destino + mapa seleccionable; origen tomado del almacén; envía coordenadas en POST/PUT. |
| `Dialogs/ClienteDialog.razor` | Campos de latitud/longitud + mapa seleccionable; persiste coordenadas. |
| `Pages/GuiaDetalle.razor` | Tarjeta “Ruta estimada” con mapa y botón de simulación. |
| `Pages/SimulacionRutas.razor` **(nuevo)** | Ruta `/simulacion-rutas`: lista de guías con ruta, KPIs (distancia, ETA, progreso, restante), controles de animación, velocidad 1x/2x/4x y bitácora de hitos simulados. |
| `Layout/NavMenu.razor` | Enlace “Simulación de rutas” en el menú principal. |

---

## 6. Flujo de la simulación

1. El usuario abre **Simulación de rutas** (o el rastreo público de una guía).
2. Se cargan las guías desde `api/rutas` (o `api/tracking/{codigo}`).
3. `MapaRutas` dibuja origen/destino y llama a OSRM con `overview=full&geometries=geojson`.
4. Si OSRM falla, se genera una polilínea estimada con factor vial urbano 1.22.
5. El marcador del vehículo se anima con `requestAnimationFrame`; cada tramo del progreso se notifica a Blazor (~9 veces/segundo).
6. La UI actualiza porcentaje, km restantes, ETA restante y registra hitos simulados (salida, tránsito, CD local, reparto final, entrega).
7. La simulación es **solo visual**: no modifica checkpoints, hitos ni entregas reales en la BD.

---

## 7. Verificación

- `dotnet build POS.slnx --nologo` → **Compilación correcta, 0 errores**.
- `database.sql` ejecutado con `sqlcmd` → BD recreada con coordenadas y datos demo.
- Servidor iniciado y verificado con `dotnet run --project POS.Server --launch-profile http` (puerto `5163`); se detuvo al finalizar la sesión de prueba. Para volver a abrirlo:

```powershell
dotnet run --project POS.Server --launch-profile http
```

### Credenciales demo

| Usuario | Contraseña | Rol |
|---|---|---|
| `ana.rojas@logitrack.test` | `LogiTrack2026!` | Administrador Logístico |
| `carlos.soliz@logitrack.test` | `LogiTrack2026!` | Conductor/Operador |

### Pruebas sugeridas

1. `/rastreo?codigo=LT-00000002` → mapa con ruta desde Almacén Central hasta Av. Cristo Redentor 250.
2. `/simulacion-rutas` → seleccionar `LT-00000002`, pulsar **Iniciar simulación**, probar pausa/reanudar/reiniciar y velocidad 4x.
3. `/guias` → nueva guía: elegir almacén, hacer clic en el mapa para fijar destino, registrar y ver la ruta en el detalle.
4. `/clientes` → editar un cliente y comprobar que el mapa muestra su coordenada guardada.

---

## 8. Notas y decisiones

- La librería Leaflet se vendoreó localmente para no depender de CDN; las teselas base sí usan OpenStreetMap (requiere conexión a internet).
- OSRM público puede limitar peticiones; el fallback garantiza que la demo funcione offline de ruteo.
- No se generó una migración EF Core porque `dotnet-ef` no está instalado y la fuente de verdad del proyecto es `database.sql`; el modelo y el mapeo ya reflejan las nuevas columnas.
- No se modificaron reglas de negocio RN01–RN14, triggers ni autorizaciones existentes.
- No se realizaron commits ni push.
