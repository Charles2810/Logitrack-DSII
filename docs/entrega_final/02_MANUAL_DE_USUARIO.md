# Manual de Usuario — LogiTrack Solutions

Guía operativa para el uso del sistema web de gestión logística y trazabilidad **LogiTrack Solutions** (`http://localhost:5163`).

---

## 1. Acceso al Sistema y Roles

Para ingresar al sistema, abra **`http://localhost:5163/login`** e ingrese su correo y contraseña.

| Rol | Correo de prueba | Contraseña | Módulos habilitados |
|---|---|---|---|
| **Administrador Logístico** | `ana.rojas@logitrack.test` | `LogiTrack2026!` | Gestión integral: Guías, Hojas de Ruta, Conductores, Flota, Clientes, Productos, Inventario, Reportes y Simulación de Rutas. |
| **Personal de Almacén** | `luis.mendoza@logitrack.test` | `LogiTrack2026!` | Productos, Inventario (entradas/salidas), Guías, Checkpoints en almacén, Conductores y Reportes. |
| **Conductor / Operador** | `carlos.soliz@logitrack.test` | `LogiTrack2026!` | Hojas de Ruta asignadas, Checkpoints en ruta, Incidencias, Entregas y Simulación de sus propias rutas. |
| **Público / Cliente** | *(No requiere inicio de sesión)* | — | Portal de rastreo público en `/rastreo` mediante código de guía. |

---

## 2. Guía Paso a Paso por Módulo

### 2.1 Clientes (`/clientes`)
1. Ingrese al menú **Clientes** para consultar, crear o editar clientes.
2. Al crear o editar un cliente, haga clic en el **mapa interactivo** para fijar su ubicación exacta (latitud y longitud).
3. Solo se pueden eliminar clientes que no tengan guías de envío registradas.

### 2.2 Productos e Inventario (`/productos` y `/inventario`)
- **Productos (`/productos`)**: Administre el catálogo (SKU, nombre, peso unitario y volumen unitario).
- **Inventario (`/inventario`)**: Consulte el stock disponible por almacén y registre movimientos de entrada o salida. El sistema impide salidas superiores al stock disponible.

### 2.3 Guías de Envío (`/guias`)
1. Haga clic en **Nueva Guía**.
2. Seleccione cliente, almacén de origen, tipo de carga y agregue los productos con sus cantidades (el sistema valida automáticamente el stock disponible).
3. En el mapa del formulario, haga clic en el punto de entrega para fijar las coordenadas de destino; el sistema calculará automáticamente la **distancia estimada (km)** y el **tiempo estimado (ETA)**.
4. Al guardar, el sistema genera un código único e inmutable (ej. `LT-00000005`).
5. Desde el botón de detalle (`/guias/{id}`) puede visualizar la tarjeta **Ruta estimada** y el historial completo de eventos.

### 2.4 Conductores y Flota (`/conductores` y `/flota`)
- **Conductores (`/conductores`)**: Permite registrar nuevos conductores, editar sus datos, restablecer su contraseña y activar/inactivar su estado operativo.
- **Flota (`/flota`)**: Permite administrar los vehículos de reparto y su disponibilidad.

### 2.5 Hojas de Ruta (`/hojas-ruta`)
1. Cree una hoja de ruta asignando un **conductor activo** y un **vehículo disponible**.
2. Asocie las guías pendientes a la hoja de ruta; el sistema notificará automáticamente al conductor asignado.

### 2.6 Operación en Ruta (`/checkpoints`, `/incidencias`, `/entregas`)
- **Checkpoints (`/checkpoints`)**: Permite avanzar el estado de la guía de forma ordenada (`Salida de Almacén` → `En Tránsito` → `En Centro de Distribución Local` → `En Reparto Final`).
- **Incidencias (`/incidencias`)**: Registra novedades o retrasos en ruta adjuntando una fotografía de evidencia (PNG/JPG/WEBP hasta 5 MB).
- **Entregas (`/entregas`)**: Registra la entrega final adjuntando la firma del receptor y fotografía de respaldo.

### 2.7 Simulación de Rutas (`/simulacion-rutas`)
1. Seleccione una guía con coordenadas (ej. `LT-00000002`).
2. Pulse **Iniciar simulación** para animar el desplazamiento del vehículo sobre la ruta vial.
3. Utilice los controles de **Pausa**, **Reanudar**, **Reiniciar** y selector de velocidad (**1x**, **2x**, **4x**). Esta vista es visual y no altera los datos reales en la base de datos.

### 2.8 Rastreo Público (`/rastreo`)
1. Cualquier cliente puede acceder sin iniciar sesión a **`http://localhost:5163/rastreo`** (o directamente `/rastreo?codigo=LT-00000002`).
2. Al ingresar el código de guía, verá el mapa de ruta origen-destino, la distancia, el tiempo estimado y la línea de tiempo de eventos, protegiendo los datos personales del destinatario.

---

## 3. Impacto en la Trazabilidad de la PYME

- **Control integral de inventario y despacho**: Evita despachos sin stock y vincula la salida de mercadería con el avance real de las guías.
- **Cero errores de secuencia operativa**: Las reglas en base de datos impiden saltos de hitos, entregas sin comprobante o doble asignación de vehículos/conductores.
- **Visibilidad para el cliente final**: Consulta pública en tiempo real con georreferenciación.
