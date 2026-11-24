# Pruebas — Rutas y mapas

Base: `http://localhost:5163` · Ana (Admin. Logístico): `ana.rojas@logitrack.test` / `LogiTrack2026!` · Carlos (Conductor): `carlos.soliz@logitrack.test` / `LogiTrack2026!`

## API

| Id | Petición | Esperado |
|---|---|---|
| R1 | `GET /api/rutas` con token de Ana | 200, 4 guías con coordenadas |
| R2 | `GET /api/rutas/2` con token de Ana | 200, `LT-00000002`, `tieneRuta=true`, origen `(-17.7837, -63.180426)`, destino `(-17.7961, -63.1649)`, `2.7 km`, `12 min` |
| R3 | `GET /api/rutas?idHojaRuta=1` | 200, 2 guías: `LT-00000002`, `LT-00000001` |
| R4 | `GET /api/rutas` con token de Carlos | 200, 2 guías (solo sus rutas) |
| R5 | `GET /api/rutas/999` | 404: `Guía no encontrada o sin coordenadas para trazar la ruta.` |
| R6 | `GET /api/rutas` sin token | 401 |
| R7 | `GET /api/tracking/LT-00000002` (público) | 200, `estadoActual=En Tránsito`, `tieneMapa=true`, `distanciaKm=2.7`, 3 eventos |

```powershell
$base = 'http://localhost:5163'
$t = (Invoke-RestMethod "$base/api/auth/login" -Method Post -ContentType 'application/json' -Body '{"correo":"ana.rojas@logitrack.test","password":"LogiTrack2026!"}').token
$h = @{ Authorization = "Bearer $t" }
Invoke-RestMethod "$base/api/rutas" -Headers $h
Invoke-RestMethod "$base/api/rutas/2" -Headers $h
Invoke-RestMethod "$base/api/rutas?idHojaRuta=1" -Headers $h
Invoke-RestMethod "$base/api/tracking/LT-00000002"
```

## Interfaz

| Id | Ruta | Paso y resultado esperado |
|---|---|---|
| M1 | `/rastreo` | Ingresar `LT-00000002` y clic en Rastrear: mapa con Almacén Central → Av. Cristo Redentor 250, ruta trazada y `2.7 km / 12 min`. |
| M2 | `/simulacion-rutas` | Seleccionar `LT-00000002`; Iniciar, Pausa, Reanudar, Reiniciar y 4x; la bitácora registra los hitos. |
| M3 | `/guias` → Nueva | Clic en el mapa fija latitud/longitud; al guardar, el detalle muestra la ruta estimada. |
| M4 | `/clientes` → Editar | El mapa muestra la coordenada guardada del cliente. |

> El trazado usa OSRM; si no responde, cae a la ruta estimada. La simulación es solo visual: no modifica la BD.

## Pruebas integradas automatizadas (.NET)

`POS.Tests` contiene 6 pruebas xUnit con `WebApplicationFactory`: validan rutas, tracking, autorización, recursos visuales y el CRUD de conductores (alta, edición y baja lógica; usa `conductor.tests@logitrack.test` y limpia lo que crea).

```powershell
cd 'C:\Users\IdeaPad Gaming 3\Documents\Proyecto\Logitrack-DSII'
dotnet test POS.slnx --nologo
```

> La solución apunta a `net9.0`, por lo que el Explorador de pruebas de Visual Studio 17.x detecta y ejecuta `POS.Tests` correctamente. También funciona `dotnet test POS.Tests/POS.Tests.csproj --nologo`. Para volver a `net10.0` se requiere Visual Studio 18.0 o superior. La BD debe estar recreada con `database.sql` y la app puede estar detenida: el host de pruebas se levanta en memoria.

## Pruebas visuales automatizadas (Playwright + Edge)

Requisitos: Node 20+ y Microsoft Edge instalado. Las pruebas usan `channel: 'msedge'`, `baseURL=http://localhost:5163` y `webServer`: inician la app si no está corriendo y reutilizan una instancia existente. La respuesta de OSRM se simula en `specs/osrm.ts` para que el trazado sea determinista y no dependa de internet.

```powershell
cd 'C:\Users\IdeaPad Gaming 3\Documents\Proyecto\Logitrack-DSII\tests\visual'
npm install
npm test
```

Cobertura:

- `rastreo.spec.ts`: rastreo público de `LT-00000002`, estado `En Tránsito`, mapa, `2.70 km` y `12 min`.
- `simulacion.spec.ts`: selección de guía, Iniciar/Pausar/Reanudar/Reiniciar y mapa visible.
- `conductores.spec.ts`: formulario nuevo, validación de contraseña inicial y cancelación sin mutar la BD.

Resultados:

```powershell
npm run report
```

El reporte HTML queda en `tests\visual\playwright-report` y las capturas en `tests\visual\test-results` (ambos ignorados por Git). También se puede abrir la UI interactiva con `npm run test:ui`.
