# Reporte Final de Pruebas y Corrección de Errores — LogiTrack Solutions

**Proyecto:** Sistema de Gestión y Trazabilidad Logística (LogiTrack Solutions)  
**Entorno verificado:** .NET `9.0.23` (`POS.slnx`) · Microsoft SQL Server (`LogiTrackDB`) · Blazor WebAssembly Hosted · Playwright + Microsoft Edge · Postman  
**Estado general:** **Aprobado — 0 errores de compilación, 100% de pruebas automatizadas superadas.**

---

## 1. Resumen Ejecutivo de Calidad

| Suite / Verificación | Herramienta | Total | Superadas | Fallidas | Duración |
|---|---|---:|---:|---:|---|
| Compilación de la solución (`POS.slnx`) | `dotnet build` (`net9.0`) | 4 proyectos | 4 | 0 | ~2.3 s |
| Pruebas de integración API y UI (`POS.Tests`) | xUnit + `WebApplicationFactory` (VS Test Explorer) | 6 | 6 | 0 | 3 s |
| Pruebas visuales E2E (`tests/visual`) | Playwright + Microsoft Edge | 3 | 3 | 0 | 7.3 s |
| Pruebas funcionales y negativas de API | Postman (`Postman_LogiTrack.json`) | 68+ peticiones | Verificadas | 0 | — |

---

## 2. Pruebas de Integración Automatizadas (`POS.Tests` — Visual Studio Test Explorer)

Ejecutadas mediante `dotnet test POS.slnx --nologo` (compatibles directamente con el **Explorador de Pruebas de Visual Studio**).

| Clase | Método de prueba | Qué valida | Resultado |
|---|---|---|---|
| `RutasMapasTests` | `Lista_y_detalle_de_rutas_incluyen_coordenadas` | `GET /api/rutas` (4 guías) y `GET /api/rutas/2` (`LT-00000002`, origen `-17.7837, -63.180426`, destino `-17.7961, -63.1649`, `2.7 km`, `12 min`). | Superada |
| `RutasMapasTests` | `Filtro_por_hoja_de_ruta_devuelve_sus_guias` | `GET /api/rutas?idHojaRuta=1` devuelve exactamente `LT-00000001` y `LT-00000002`. | Superada |
| `RutasMapasTests` | `Tracking_publico_expone_mapa_sin_token` | `GET /api/tracking/LT-00000002` sin autenticación devuelve estado `En Tránsito`, mapa activo, `2.7 km`, `12 min` y 3 eventos. | Superada |
| `RutasMapasTests` | `Rutas_exigen_token_y_el_conductor_solo_ve_las_suyas` | Devuelve `401 Unauthorized` sin token y filtra por alcance del conductor (`carlos.soliz@logitrack.test` ve solo sus 2 guías). | Superada |
| `RutasMapasTests` | `Recursos_visuales_y_rutas_de_la_ui_responden` | Responden `200 OK` las rutas `/`, `/conductores`, `/rastreo?codigo=LT-00000002`, `/simulacion-rutas`, `/js/mapa.js` y Leaflet local. | Superada |
| `ConductoresTests` | `Crud_de_conductores_con_baja_logica_y_autorizacion` | Alta, listado, edición, reset de contraseña, bloqueo `403 Forbidden` para rol Conductor y baja lógica (`Activo = false`). | Superada |

**Resultado registrado:**
```text
Correctas! - Con error: 0, Superado: 6, Omitido: 0, Total: 6, Duración: 3 s - POS.Tests.dll (net9.0)
```

---

## 3. Pruebas Visuales E2E (`tests/visual` — Playwright + Microsoft Edge)

Ejecutadas mediante `npm --prefix tests/visual test` contra el servidor activo en `http://localhost:5163` (con respuesta OSRM simulada en `specs/osrm.ts` para trazado determinista).

| Archivo Spec | Caso probado | Resultado | Duración |
|---|---|---|---|
| `specs/conductores.spec.ts` | Formulario de conductores valida la contraseña inicial y cancela sin mutar la BD. | Superada (`ok 1`) | 2.3 s |
| `specs/rastreo.spec.ts` | Rastreo público de `LT-00000002` muestra mapa Leaflet, `2.70 km`, `12 min` y timeline de hitos. | Superada (`ok 2`) | 1.8 s |
| `specs/simulacion.spec.ts` | Simulación de rutas selecciona guía, inicia, pausa, reanuda y reinicia animación sobre el mapa. | Superada (`ok 3`) | 2.2 s |

**Resultado registrado:**
```text
3 passed (7.3s)
```
El reporte HTML detallado se genera en `tests/visual/playwright-report` (visualizable con `npm --prefix tests/visual run report`).

---

## 4. Validación de Reglas de Negocio y API REST (Postman / SQL Server)

La colección `Postman_LogiTrack.json` (17 carpetas, 68+ peticiones) y los triggers de `LogiTrackDB` validan las reglas críticas de la PYME:

| Regla | Mecanismo de validación | Comportamiento verificado |
|---|---|---|
| **RN01** Código único e inmutable | Columna calculada + `UNIQUE` en `guia_envio` | Código autogenerado (`LT-00000001`), imposible de alterar manualmente. |
| **RN03 / RN04** Asignación y recursos | Triggers en `hoja_ruta` y `guia_envio` | Una guía no puede pertenecer a dos hojas activas; conductores/vehículos inactivos son rechazados. |
| **RN05 / RN06** Avance ordenado de hitos | Trigger en `checkpoint` | Impide saltar hitos (ej. pasar de `Registrada` a `Entregado` responde `400`). |
| **RN07 / RN08** Comprobantes e incidencias | `CHECK` + triggers + `/api/files/upload` | Entrega exige firma/foto; archivos validados en formato (PNG/JPG/WEBP) y tamaño ($\le$ 5 MB). |
| **RN10** Consulta pública segura | Vista `vw_trazabilidad_publica` + `TrackingController` | Expone hitos y coordenadas sin filtrar datos personales del cliente/destinatario. |
| **RN11 / RN12 / RN13** Seguridad y roles | JWT Bearer + triggers de rol | Usuarios inactivos (`sofia.campos@logitrack.test`) bloqueados (`401`); conductores solo operan sus guías (`403` si intentan otra). |
| **RNF05** Integridad y auditoría | Triggers de bloqueo + validaciones API | Impide eliminar clientes con guías (`409`), productos con stock (`409`) o guías ya despachadas (`409`). |

---

## 5. Registro de Errores Detectados y Corregidos

Durante las iteraciones de desarrollo, integración y pruebas se identificaron y resolvieron los siguientes defectos:

| ID | Error detectado | Causa raíz | Solución aplicada | Referencia |
|---|---|---|---|---|
| **ERR-01** | Al entrar al rastreo público (`/rastreo`) estando autenticado, se perdía la sesión del usuario. | El flujo público limpiaba/reiniciaba el estado de autenticación en el cliente Blazor. | Se preservó el token JWT en `localStorage` al consultar rutas públicas sin exigir login. | Commit `01d9df0` |
| **ERR-02** | Creación de guías sin validación previa de stock disponible y mensajes de error genéricos en UI. | El formulario no verificaba existencias por almacén ni propagaba el detalle de error SQL/API. | Se añadió validación de stock por almacén al crear guías y visualización clara de errores en `GuiaDialog.razor`. | Commit `53f9c58` |
| **ERR-03** | Descuentos de inventario desfasados y operaciones CRUD sin protección transaccional completa. | El descuento no estaba amarrado al checkpoint operativo ni existían controles uniformes de eliminación. | Se implementó descuento transaccional por checkpoint y bloqueos de integridad referencial (`409 Conflict`). | Commit `9de4606` |
| **ERR-04** | El Explorador de Pruebas de Visual Studio 17.x no detectaba/ejecutaba `POS.Tests` y el equipo local requería runtime compatible. | Los proyectos apuntaban a `net10.0` (preview/VS 18). | Se ajustó `TargetFramework` a `net9.0` en toda la solución y se instaló `.NET 9.0.23` (`AspNetCore.App` / `NETCore.App`). | Commit `ae494f1` |
| **ERR-05** | Capturas de evidencia rotas al mover documentos de la raíz a `docs/`. | `Documento_Arquitectura_ConexionBD_Auth.md` usaba rutas `./docs/images/...`. | Se actualizaron los enlaces relativos a `./images/...` tras centralizar la documentación en `docs/`. | Commit `5168811` |
| **ERR-06** | `sqlcmd` rechazaba la conexión local con error SSL de certificado no confiable. | Microsoft ODBC Driver 18 habilita cifrado estricto por defecto en conexiones locales. | Se estandarizó la ejecución de `database.sql` usando el parámetro `-C` (`TrustServerCertificate`). | Despliegue local BD |
