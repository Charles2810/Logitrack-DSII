# Guion de Demostración en Vivo y Defensa Oral (5 minutos) — LogiTrack Solutions

Guía práctica para presentar el proyecto frente al docente y responder con seguridad a los criterios de la rúbrica.

---

## Orden Recomendado de la Demostración (5 min)

### 1. Base de Datos y Arranque (30 s)
- Mostrar `LogiTrackDB` en SQL Server (18 tablas, 2 vistas, **23 triggers** que blindan las reglas de negocio RN01–RN14).
- Levantar la aplicación (si no está abierta):
  ```powershell
  dotnet run --project POS.Server --launch-profile http
  ```
- Abrir **`http://localhost:5163`**.

### 2. Rastreo Público sin Login (45 s)
- Ir a **`http://localhost:5163/rastreo?codigo=LT-00000002`**.
- Destacar que el cliente final puede consultar su envío en tiempo real con **mapa Leaflet**, distancia (`2.70 km`), ETA (`12 min`) y línea de tiempo, sin exponer datos personales (RN10).

### 3. Gestión Administrativa, Stock y Georreferenciación (1 min 30 s)
- Iniciar sesión como **`ana.rojas@logitrack.test`** / **`LogiTrack2026!`**.
- Abrir **Guías → Nueva Guía**: mostrar la validación automática de stock por almacén y la selección de destino haciendo clic en el mapa.
- Abrir **Conductores (`/conductores`)**: mostrar el CRUD completo y el control de estado operativo.

### 4. Simulación de Entregas en Ruta (45 s)
- Entrar a **Simulación de rutas (`/simulacion-rutas`)**, seleccionar `LT-00000002` y pulsar **Iniciar simulación** (probar velocidad `4x`), explicando que es una vista visual que no altera la BD real.

### 5. Evidencia de Pruebas y Calidad (1 min 30 s)
- **Visual Studio Test Explorer (`POS.Tests`)**: mostrar los **6 checkmarks verdes** (`ConductoresTests` y `RutasMapasTests`, `WebApplicationFactory` conectado a SQL Server).
- **Pruebas Visuales E2E (Playwright)**: mostrar el reporte HTML con **`npm --prefix tests/visual run report`** (o correr `npm --prefix tests/visual run test:ui`).
- **Postman (`Postman_LogiTrack.json`)**: mostrar una petición rápida con token JWT automático o una prueba negativa (ej. `403` o `409`).
