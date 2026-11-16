/* ============================================================
   LogiTrack Solutions - Base de datos LogiTrackDB
   Motor: Microsoft SQL Server 2019 o superior
   Proyecto: Sistema de gestión y trazabilidad logística
   ------------------------------------------------------------
   Este script RECREA la base de datos (borra la existente).
   Trazabilidad de reglas implementadas en BD:
     RN01  código único e inmutable  -> columna calculada + UQ
     RN02  datos mínimos             -> columnas NOT NULL + CHECK
     RN03  una hoja de ruta por guía -> trigger de asignación
     RN04  recursos habilitados      -> triggers hoja_ruta y usuario
     RN05  avance ordenado de hitos  -> trigger checkpoint
     RN06  checkpoint válido         -> trigger checkpoint
     RN07  entrega con comprobante   -> CHECK + trigger entrega
     RN08  incidencia documentada    -> NOT NULL + CHECK + trigger
     RN09  notificaciones internas   -> triggers de notificación
     RN10  consulta pública          -> vista sin datos personales
     RN11  usuarios no se eliminan   -> trigger usuario
     RN12  separación de funciones   -> triggers de rol
     RN13  alcance del conductor     -> triggers de checkpoint/incidencia/entrega
     RNF05 integridad y auditoría    -> bloqueo de guías entregadas y eventos
   ============================================================ */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

USE master;
GO

IF DB_ID(N'LogiTrackDB') IS NOT NULL
BEGIN
    ALTER DATABASE LogiTrackDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LogiTrackDB;
END
GO

CREATE DATABASE LogiTrackDB COLLATE Modern_Spanish_CI_AS;
GO

USE LogiTrackDB;
GO

/* ============================================================
   1. SEGURIDAD
   ============================================================ */
CREATE TABLE dbo.rol (
    id_rol  INT IDENTITY(1,1) CONSTRAINT PK_rol PRIMARY KEY,
    nombre  NVARCHAR(40) NOT NULL CONSTRAINT UQ_rol_nombre UNIQUE
);

CREATE TABLE dbo.usuario (
    id_usuario      INT IDENTITY(1,1) CONSTRAINT PK_usuario PRIMARY KEY,
    id_rol          INT NOT NULL CONSTRAINT FK_usuario_rol REFERENCES dbo.rol(id_rol),
    nombre_completo NVARCHAR(120) NOT NULL,
    correo          NVARCHAR(120) NOT NULL CONSTRAINT UQ_usuario_correo UNIQUE,
    password_hash   NVARCHAR(255) NOT NULL,
    activo          BIT NOT NULL CONSTRAINT DF_usuario_activo DEFAULT (1),
    fecha_creacion  DATETIME2(0) NOT NULL CONSTRAINT DF_usuario_fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_usuario_nombre CHECK (LEN(LTRIM(RTRIM(nombre_completo))) > 0),
    CONSTRAINT CK_usuario_correo CHECK (correo LIKE N'%_@_%._%')
);

/* ============================================================
   2. CATÁLOGOS Y MAESTROS
   ============================================================ */
CREATE TABLE dbo.cliente (
    id_cliente   INT IDENTITY(1,1) CONSTRAINT PK_cliente PRIMARY KEY,
    razon_social NVARCHAR(150) NOT NULL,
    nit          NVARCHAR(30) NOT NULL CONSTRAINT UQ_cliente_nit UNIQUE,
    telefono     NVARCHAR(25) NULL,
    direccion    NVARCHAR(200) NULL,
    CONSTRAINT CK_cliente_razon CHECK (LEN(LTRIM(RTRIM(razon_social))) > 0),
    CONSTRAINT CK_cliente_nit CHECK (LEN(LTRIM(RTRIM(nit))) > 0)
);

CREATE TABLE dbo.almacen (
    id_almacen INT IDENTITY(1,1) CONSTRAINT PK_almacen PRIMARY KEY,
    nombre     NVARCHAR(100) NOT NULL CONSTRAINT UQ_almacen_nombre UNIQUE,
    tipo       NVARCHAR(30) NOT NULL
               CONSTRAINT CK_almacen_tipo CHECK (tipo IN (N'Almacén', N'Centro de Distribución')),
    ciudad     NVARCHAR(80) NOT NULL,
    direccion  NVARCHAR(200) NULL
);

CREATE TABLE dbo.tipo_carga (
    id_tipo_carga INT IDENTITY(1,1) CONSTRAINT PK_tipo_carga PRIMARY KEY,
    nombre        NVARCHAR(60) NOT NULL CONSTRAINT UQ_tipo_carga_nombre UNIQUE
);

CREATE TABLE dbo.hito (
    id_hito INT IDENTITY(1,1) CONSTRAINT PK_hito PRIMARY KEY,
    nombre  NVARCHAR(60) NOT NULL CONSTRAINT UQ_hito_nombre UNIQUE,
    orden   TINYINT NOT NULL CONSTRAINT UQ_hito_orden UNIQUE
);

CREATE TABLE dbo.tipo_incidencia (
    id_tipo_incidencia INT IDENTITY(1,1) CONSTRAINT PK_tipo_incidencia PRIMARY KEY,
    nombre             NVARCHAR(80) NOT NULL CONSTRAINT UQ_tipo_incidencia_nombre UNIQUE
);

CREATE TABLE dbo.vehiculo (
    id_vehiculo  INT IDENTITY(1,1) CONSTRAINT PK_vehiculo PRIMARY KEY,
    placa        NVARCHAR(15) NOT NULL CONSTRAINT UQ_vehiculo_placa UNIQUE,
    tipo         NVARCHAR(40) NOT NULL,
    capacidad_kg DECIMAL(10,2) NOT NULL CONSTRAINT CK_vehiculo_kg CHECK (capacidad_kg > 0),
    capacidad_m3 DECIMAL(10,2) NOT NULL CONSTRAINT CK_vehiculo_m3 CHECK (capacidad_m3 > 0),
    estado       NVARCHAR(20) NOT NULL CONSTRAINT DF_vehiculo_estado DEFAULT (N'Disponible')
                 CONSTRAINT CK_vehiculo_estado
                 CHECK (estado IN (N'Disponible', N'En ruta', N'Mantenimiento', N'Inhabilitado'))
);

/* ============================================================
   3. INVENTARIO
   ============================================================ */
CREATE TABLE dbo.producto (
    id_producto         INT IDENTITY(1,1) CONSTRAINT PK_producto PRIMARY KEY,
    sku                 NVARCHAR(40) NOT NULL CONSTRAINT UQ_producto_sku UNIQUE,
    nombre              NVARCHAR(150) NOT NULL,
    peso_unitario_kg    DECIMAL(10,3) NOT NULL CONSTRAINT CK_producto_peso CHECK (peso_unitario_kg > 0),
    volumen_unitario_m3 DECIMAL(10,4) NOT NULL CONSTRAINT CK_producto_vol CHECK (volumen_unitario_m3 > 0)
);

CREATE TABLE dbo.inventario (
    id_almacen   INT NOT NULL CONSTRAINT FK_inventario_almacen  REFERENCES dbo.almacen(id_almacen),
    id_producto  INT NOT NULL CONSTRAINT FK_inventario_producto REFERENCES dbo.producto(id_producto),
    stock_actual INT NOT NULL CONSTRAINT DF_inventario_stock DEFAULT (0)
                 CONSTRAINT CK_inventario_stock CHECK (stock_actual >= 0),
    CONSTRAINT PK_inventario PRIMARY KEY (id_almacen, id_producto)
);

/* ============================================================
   4. DISTRIBUCIÓN Y TRAZABILIDAD
   ============================================================ */
CREATE TABLE dbo.hoja_ruta (
    id_hoja_ruta        INT IDENTITY(1,1) CONSTRAINT PK_hoja_ruta PRIMARY KEY,
    id_vehiculo         INT NOT NULL CONSTRAINT FK_hoja_vehiculo  REFERENCES dbo.vehiculo(id_vehiculo),
    id_conductor        INT NOT NULL CONSTRAINT FK_hoja_conductor REFERENCES dbo.usuario(id_usuario),
    id_usuario_creador  INT NOT NULL CONSTRAINT FK_hoja_creador   REFERENCES dbo.usuario(id_usuario),
    estado              NVARCHAR(20) NOT NULL CONSTRAINT DF_hoja_estado DEFAULT (N'Pendiente')
                        CONSTRAINT CK_hoja_estado CHECK (estado IN (N'Pendiente', N'En curso', N'Finalizada')),
    fecha_creacion      DATETIME2(0) NOT NULL CONSTRAINT DF_hoja_fecha DEFAULT (SYSDATETIME())
);

CREATE TABLE dbo.guia_envio (
    id_guia             INT IDENTITY(1,1) CONSTRAINT PK_guia_envio PRIMARY KEY,
    codigo_guia         AS (N'LT-' + RIGHT(N'00000000' + CAST(id_guia AS NVARCHAR(10)), 8)) PERSISTED,
    id_cliente          INT NOT NULL CONSTRAINT FK_guia_cliente REFERENCES dbo.cliente(id_cliente),
    id_almacen_origen   INT NOT NULL CONSTRAINT FK_guia_origen  REFERENCES dbo.almacen(id_almacen),
    id_tipo_carga       INT NOT NULL CONSTRAINT FK_guia_tipo    REFERENCES dbo.tipo_carga(id_tipo_carga),
    id_hito_actual      INT NOT NULL CONSTRAINT DF_guia_hito DEFAULT (1)  -- 1 = Registrada
                        CONSTRAINT FK_guia_hito REFERENCES dbo.hito(id_hito),
    id_hoja_ruta        INT NULL CONSTRAINT FK_guia_hoja REFERENCES dbo.hoja_ruta(id_hoja_ruta),
    id_usuario_registro INT NOT NULL CONSTRAINT FK_guia_usuario REFERENCES dbo.usuario(id_usuario),
    destinatario        NVARCHAR(120) NOT NULL,
    direccion_destino   NVARCHAR(200) NOT NULL,
    peso_kg             DECIMAL(10,2) NOT NULL CONSTRAINT CK_guia_peso CHECK (peso_kg > 0),
    volumen_m3          DECIMAL(10,3) NOT NULL CONSTRAINT CK_guia_vol  CHECK (volumen_m3 > 0),
    fecha_registro      DATETIME2(0) NOT NULL CONSTRAINT DF_guia_fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_guia_codigo UNIQUE (codigo_guia),
    CONSTRAINT CK_guia_destinatario CHECK (LEN(LTRIM(RTRIM(destinatario))) > 0),
    CONSTRAINT CK_guia_direccion CHECK (LEN(LTRIM(RTRIM(direccion_destino))) > 0)
);

CREATE TABLE dbo.detalle_guia (
    id_guia      INT NOT NULL CONSTRAINT FK_detalle_guia     REFERENCES dbo.guia_envio(id_guia),
    id_producto  INT NOT NULL CONSTRAINT FK_detalle_producto REFERENCES dbo.producto(id_producto),
    cantidad     INT NOT NULL CONSTRAINT CK_detalle_cantidad CHECK (cantidad > 0),
    CONSTRAINT PK_detalle_guia PRIMARY KEY (id_guia, id_producto)
);

CREATE TABLE dbo.movimiento_inventario (
    id_movimiento   INT IDENTITY(1,1) CONSTRAINT PK_movimiento PRIMARY KEY,
    id_almacen      INT NOT NULL CONSTRAINT FK_mov_almacen  REFERENCES dbo.almacen(id_almacen),
    id_producto     INT NOT NULL CONSTRAINT FK_mov_producto REFERENCES dbo.producto(id_producto),
    id_guia         INT NULL CONSTRAINT FK_mov_guia REFERENCES dbo.guia_envio(id_guia),
    id_usuario      INT NOT NULL CONSTRAINT FK_mov_usuario REFERENCES dbo.usuario(id_usuario),
    tipo_movimiento NVARCHAR(10) NOT NULL
                    CONSTRAINT CK_mov_tipo CHECK (tipo_movimiento IN (N'ENTRADA', N'SALIDA')),
    cantidad        INT NOT NULL CONSTRAINT CK_mov_cantidad CHECK (cantidad > 0),
    fecha_hora      DATETIME2(0) NOT NULL CONSTRAINT DF_mov_fecha DEFAULT (SYSDATETIME())
);

CREATE TABLE dbo.[checkpoint] (
    id_checkpoint INT IDENTITY(1,1) CONSTRAINT PK_checkpoint PRIMARY KEY,
    id_guia       INT NOT NULL CONSTRAINT FK_check_guia   REFERENCES dbo.guia_envio(id_guia),
    id_hito       INT NOT NULL CONSTRAINT FK_check_hito   REFERENCES dbo.hito(id_hito),
    id_almacen    INT NULL CONSTRAINT FK_check_almacen    REFERENCES dbo.almacen(id_almacen),
    id_usuario    INT NOT NULL CONSTRAINT FK_check_usuario REFERENCES dbo.usuario(id_usuario),
    fecha_hora    DATETIME2(0) NOT NULL CONSTRAINT DF_check_fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_checkpoint_guia_hito UNIQUE (id_guia, id_hito)
);

CREATE TABLE dbo.incidencia (
    id_incidencia      INT IDENTITY(1,1) CONSTRAINT PK_incidencia PRIMARY KEY,
    id_guia            INT NOT NULL CONSTRAINT FK_inc_guia    REFERENCES dbo.guia_envio(id_guia),
    id_tipo_incidencia INT NOT NULL CONSTRAINT FK_inc_tipo    REFERENCES dbo.tipo_incidencia(id_tipo_incidencia),
    id_usuario         INT NOT NULL CONSTRAINT FK_inc_usuario REFERENCES dbo.usuario(id_usuario),
    descripcion        NVARCHAR(500) NOT NULL,
    ruta_foto          NVARCHAR(300) NOT NULL,          -- RN08: evidencia obligatoria
    fecha_hora         DATETIME2(0) NOT NULL CONSTRAINT DF_inc_fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_incidencia_desc CHECK (LEN(LTRIM(RTRIM(descripcion))) > 0),
    CONSTRAINT CK_incidencia_foto CHECK (LEN(LTRIM(RTRIM(ruta_foto))) > 0)
);

CREATE TABLE dbo.entrega (
    id_entrega      INT IDENTITY(1,1) CONSTRAINT PK_entrega PRIMARY KEY,
    id_guia         INT NOT NULL CONSTRAINT UQ_entrega_guia UNIQUE
                    CONSTRAINT FK_entrega_guia REFERENCES dbo.guia_envio(id_guia),
    id_usuario      INT NOT NULL CONSTRAINT FK_entrega_usuario REFERENCES dbo.usuario(id_usuario),
    nombre_receptor NVARCHAR(120) NULL,
    ruta_firma      NVARCHAR(300) NULL,
    ruta_foto       NVARCHAR(300) NULL,
    fecha_hora      DATETIME2(0) NOT NULL CONSTRAINT DF_entrega_fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_entrega_comprobante CHECK (   -- RN07
        (ruta_firma IS NOT NULL AND LEN(LTRIM(RTRIM(ruta_firma))) > 0)
        OR (ruta_foto IS NOT NULL AND LEN(LTRIM(RTRIM(ruta_foto))) > 0)
    )
);

CREATE TABLE dbo.notificacion (
    id_notificacion INT IDENTITY(1,1) CONSTRAINT PK_notificacion PRIMARY KEY,
    id_usuario      INT NOT NULL CONSTRAINT FK_notif_usuario REFERENCES dbo.usuario(id_usuario),
    id_guia         INT NULL CONSTRAINT FK_notif_guia REFERENCES dbo.guia_envio(id_guia),
    tipo            NVARCHAR(30) NOT NULL
                    CONSTRAINT CK_notif_tipo
                    CHECK (tipo IN (N'Nueva hoja de ruta', N'Incidencia', N'Entrega finalizada')),
    mensaje         NVARCHAR(300) NOT NULL,
    leida           BIT NOT NULL CONSTRAINT DF_notif_leida DEFAULT (0),
    fecha_hora      DATETIME2(0) NOT NULL CONSTRAINT DF_notif_fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_notif_mensaje CHECK (LEN(LTRIM(RTRIM(mensaje))) > 0)
);
GO

/* ============================================================
   5. ÍNDICES
   ============================================================ */
CREATE INDEX IX_guia_cliente       ON dbo.guia_envio (id_cliente);
CREATE INDEX IX_guia_hito          ON dbo.guia_envio (id_hito_actual);
CREATE INDEX IX_guia_hoja          ON dbo.guia_envio (id_hoja_ruta);
CREATE INDEX IX_guia_fecha         ON dbo.guia_envio (fecha_registro);
CREATE INDEX IX_guia_estado_fecha  ON dbo.guia_envio (id_hito_actual, fecha_registro);
CREATE INDEX IX_hoja_conductor     ON dbo.hoja_ruta (id_conductor, estado);
CREATE INDEX IX_hoja_vehiculo      ON dbo.hoja_ruta (id_vehiculo, estado);
CREATE INDEX IX_checkpoint_guia    ON dbo.[checkpoint] (id_guia, fecha_hora);
CREATE INDEX IX_incidencia_guia    ON dbo.incidencia (id_guia, fecha_hora);
CREATE INDEX IX_entrega_usuario    ON dbo.entrega (id_usuario, fecha_hora);
CREATE INDEX IX_notif_usuario      ON dbo.notificacion (id_usuario, leida);
CREATE INDEX IX_mov_almacen_prod   ON dbo.movimiento_inventario (id_almacen, id_producto, fecha_hora);
GO

/* ============================================================
   6. VISTAS
   ============================================================ */

/* RN10 / RF05 / RNF03: línea de tiempo pública sin datos personales.
   Eventos: Registrada (orden 0), checkpoints y entrega final. */
CREATE VIEW dbo.vw_trazabilidad_publica AS
SELECT g.codigo_guia,
       ha.nombre      AS estado_actual,
       N'Registrada'  AS tipo_evento,
       h0.nombre      AS hito,
       h0.orden       AS orden_hito,
       g.fecha_registro AS fecha_hora,
       ao.nombre      AS estacion
FROM dbo.guia_envio g
JOIN dbo.hito    ha ON ha.id_hito = g.id_hito_actual
JOIN dbo.hito    h0 ON h0.nombre  = N'Registrada'
JOIN dbo.almacen ao ON ao.id_almacen = g.id_almacen_origen
UNION ALL
SELECT g.codigo_guia,
       ha.nombre,
       N'Checkpoint',
       hc.nombre,
       hc.orden,
       c.fecha_hora,
       a.nombre
FROM dbo.guia_envio g
JOIN dbo.hito      ha ON ha.id_hito = g.id_hito_actual
JOIN dbo.[checkpoint] c ON c.id_guia = g.id_guia
JOIN dbo.hito      hc ON hc.id_hito = c.id_hito
LEFT JOIN dbo.almacen a ON a.id_almacen = c.id_almacen
UNION ALL
SELECT g.codigo_guia,
       ha.nombre,
       N'Entrega',
       he.nombre,
       he.orden,
       e.fecha_hora,
       CAST(NULL AS NVARCHAR(100)) AS estacion
FROM dbo.guia_envio g
JOIN dbo.hito    ha ON ha.id_hito = g.id_hito_actual
JOIN dbo.entrega e  ON e.id_guia = g.id_guia
JOIN dbo.hito    he ON he.nombre = N'Entregado';
GO

/* RF08 / RNF09: datos internos para reportes e indicadores. */
CREATE VIEW dbo.vw_reporte_envios AS
SELECT g.id_guia,
       g.codigo_guia,
       g.fecha_registro,
       c.razon_social        AS cliente,
       ao.nombre             AS almacen_origen,
       tc.nombre             AS tipo_carga,
       g.destinatario,
       g.direccion_destino,
       g.peso_kg,
       g.volumen_m3,
       ha.nombre             AS estado_actual,
       h.estado              AS estado_hoja_ruta,
       v.placa               AS vehiculo,
       uc.nombre_completo    AS conductor,
       e.fecha_hora          AS fecha_entrega,
       CASE WHEN e.fecha_hora IS NOT NULL
            THEN DATEDIFF(MINUTE, g.fecha_registro, e.fecha_hora) / 60.0
            ELSE NULL END    AS horas_transito,
       (SELECT COUNT(*) FROM dbo.incidencia i WHERE i.id_guia = g.id_guia) AS total_incidencias
FROM dbo.guia_envio g
JOIN dbo.cliente    c  ON c.id_cliente = g.id_cliente
JOIN dbo.almacen    ao ON ao.id_almacen = g.id_almacen_origen
JOIN dbo.tipo_carga tc ON tc.id_tipo_carga = g.id_tipo_carga
JOIN dbo.hito       ha ON ha.id_hito = g.id_hito_actual
LEFT JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = g.id_hoja_ruta
LEFT JOIN dbo.vehiculo  v ON v.id_vehiculo = h.id_vehiculo
LEFT JOIN dbo.usuario   uc ON uc.id_usuario = h.id_conductor
LEFT JOIN dbo.entrega   e  ON e.id_guia = g.id_guia;
GO

/* ============================================================
   7. DISPARADORES DE REGLAS DE NEGOCIO
   ============================================================ */

-- RN11: las cuentas se inhabilitan, no se eliminan.
CREATE TRIGGER dbo.trg_usuario_no_delete ON dbo.usuario
INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50010, N'RN11: los usuarios no se eliminan; deben inhabilitarse (activo = 0).', 1;
END
GO

-- RN04: un conductor con hoja de ruta activa no puede inhabilitarse ni cambiar
-- a un rol distinto de Conductor/Operador.
CREATE TRIGGER dbo.trg_usuario_estado_valida ON dbo.usuario
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.id_usuario = i.id_usuario
        JOIN dbo.hoja_ruta h ON h.id_conductor = i.id_usuario
        WHERE (i.activo = 0 OR i.id_rol <> d.id_rol)
          AND h.estado IN (N'Pendiente', N'En curso')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50024, N'RN04: el conductor tiene una hoja de ruta activa; no puede inhabilitarse ni cambiar de rol.', 1;
    END
END
GO

-- RN04 / RN12: hoja de ruta con conductor y vehículo habilitados; la crea
-- únicamente el Administrador Logístico y no puede haber dos hojas activas
-- para el mismo vehículo o conductor.
CREATE TRIGGER dbo.trg_hoja_ruta_valida ON dbo.hoja_ruta
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.usuario  uc ON uc.id_usuario = i.id_usuario_creador
        JOIN dbo.rol      rc ON rc.id_rol = uc.id_rol
        JOIN dbo.usuario  u  ON u.id_usuario = i.id_conductor
        JOIN dbo.rol      r  ON r.id_rol = u.id_rol
        JOIN dbo.vehiculo v  ON v.id_vehiculo = i.id_vehiculo
        WHERE i.estado <> N'Finalizada'
          AND (uc.activo = 0
               OR rc.nombre <> N'Administrador Logístico'
               OR u.activo = 0
               OR r.nombre <> N'Conductor/Operador'
               OR v.estado IN (N'Mantenimiento', N'Inhabilitado'))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50004, N'RN04/RN12: el conductor, el vehículo o el usuario creador no están habilitados para la hoja de ruta.', 1;
    END

    -- RNF05: una hoja finalizada no se modifica.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.id_hoja_ruta = i.id_hoja_ruta
        WHERE d.estado = N'Finalizada'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50008, N'RNF05: una hoja de ruta finalizada no puede modificarse.', 1;
    END

    -- RN05: una hoja solo se finaliza cuando todas sus guías están entregadas.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.id_hoja_ruta = i.id_hoja_ruta
        WHERE i.estado = N'Finalizada'
          AND d.estado <> N'Finalizada'
          AND EXISTS (
              SELECT 1
              FROM dbo.guia_envio g
              JOIN dbo.hito h ON h.id_hito = g.id_hito_actual
              WHERE g.id_hoja_ruta = i.id_hoja_ruta
                AND h.nombre <> N'Entregado'
          )
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50023, N'RN05: no se puede finalizar una hoja de ruta con guías pendientes de entrega.', 1;
    END

    -- RN04: un vehículo y un conductor solo pueden tener una hoja activa.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.hoja_ruta h
          ON h.id_hoja_ruta <> i.id_hoja_ruta
         AND (h.id_vehiculo = i.id_vehiculo OR h.id_conductor = i.id_conductor)
        WHERE i.estado IN (N'Pendiente', N'En curso')
          AND h.estado IN (N'Pendiente', N'En curso')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'RN04: el vehículo o el conductor ya tienen otra hoja de ruta activa.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN inserted j
          ON j.id_hoja_ruta <> i.id_hoja_ruta
         AND (j.id_vehiculo = i.id_vehiculo OR j.id_conductor = i.id_conductor)
        WHERE i.estado IN (N'Pendiente', N'En curso')
          AND j.estado IN (N'Pendiente', N'En curso')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'RN04: dos hojas de ruta del mismo lote comparten vehículo o conductor.', 1;
    END
END
GO

-- RN09: notifica al conductor cuando se le asigna una hoja de ruta.
CREATE TRIGGER dbo.trg_hoja_ruta_notifica ON dbo.hoja_ruta
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.notificacion (id_usuario, id_guia, tipo, mensaje)
    SELECT i.id_conductor,
           NULL,
           N'Nueva hoja de ruta',
           CONCAT(N'Se te asignó la hoja de ruta #', i.id_hoja_ruta,
                  N' para el vehículo ', v.placa, N'.')
    FROM inserted i
    JOIN dbo.vehiculo v ON v.id_vehiculo = i.id_vehiculo;
END
GO

-- RN02 / RN03 / RN05 / RN12 / RNF05:
--   * Registro de guías solo por Administrador Logístico.
--   * La guía nace en "Registrada".
--   * El hito solo avanza +1 y con un checkpoint/entrega que lo respalde.
--   * Una guía solo se asigna a una hoja de ruta cuando está pendiente de despacho.
--   * No se excede la capacidad del vehículo.
CREATE TRIGGER dbo.trg_guia_envio_valida ON dbo.guia_envio
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hito_registrada INT = (SELECT id_hito FROM dbo.hito WHERE nombre = N'Registrada');

    -- RN12: solo el Administrador Logístico registra guías.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN deleted d ON d.id_guia = i.id_guia
        JOIN dbo.usuario u ON u.id_usuario = i.id_usuario_registro
        JOIN dbo.rol     r ON r.id_rol = u.id_rol
        WHERE (d.id_guia IS NULL OR d.id_usuario_registro <> i.id_usuario_registro)
          AND (u.activo = 0 OR r.nombre <> N'Administrador Logístico')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50018, N'RN12: solo un Administrador Logístico activo puede registrar guías.', 1;
    END

    -- RN05: toda guía nueva inicia en "Registrada".
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN deleted d ON d.id_guia = i.id_guia
        WHERE d.id_guia IS NULL
          AND i.id_hito_actual <> @hito_registrada
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50022, N'RN05: una guía nueva debe registrarse en el hito Registrada.', 1;
    END

    -- RN05 / RNF05: el hito solo avanza al siguiente y con evento de respaldo.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.id_guia = i.id_guia
        JOIN dbo.hito ho ON ho.id_hito = d.id_hito_actual
        JOIN dbo.hito hn ON hn.id_hito = i.id_hito_actual
        WHERE i.id_hito_actual <> d.id_hito_actual
          AND (
                hn.orden <> ho.orden + 1
             OR (hn.nombre =  N'Entregado' AND NOT EXISTS (
                    SELECT 1 FROM dbo.entrega e WHERE e.id_guia = i.id_guia))
             OR (hn.nombre <> N'Entregado' AND NOT EXISTS (
                    SELECT 1 FROM dbo.[checkpoint] c
                    WHERE c.id_guia = i.id_guia AND c.id_hito = i.id_hito_actual))
          )
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, N'RN05/RNF05: el hito solo puede avanzar al siguiente y debe existir el checkpoint o la entrega que lo respalde.', 1;
    END

    -- RN03: solo se desasigna una guía antes de salir de almacén.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.id_guia = i.id_guia
        JOIN dbo.hito hi ON hi.id_hito = i.id_hito_actual
        WHERE i.id_hoja_ruta IS NULL
          AND d.id_hoja_ruta IS NOT NULL
          AND (hi.nombre <> N'Registrada'
               OR EXISTS (SELECT 1 FROM dbo.[checkpoint] c WHERE c.id_guia = i.id_guia))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'RN03: una guía despachada no puede quitarse de su hoja de ruta.', 1;
    END

    -- RN03: solo las guías registradas y pendientes pueden asignarse;
    -- no se reasigna una guía que ya pertenece a otra hoja de ruta.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN deleted d ON d.id_guia = i.id_guia
        LEFT JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = i.id_hoja_ruta
        JOIN dbo.hito hi ON hi.id_hito = i.id_hito_actual
        WHERE i.id_hoja_ruta IS NOT NULL
          AND (d.id_guia IS NULL OR ISNULL(d.id_hoja_ruta, 0) <> i.id_hoja_ruta)
          AND (
                h.id_hoja_ruta IS NULL
             OR h.estado = N'Finalizada'
             OR (d.id_guia IS NULL AND hi.nombre <> N'Registrada')
             OR (d.id_guia IS NOT NULL AND d.id_hoja_ruta IS NOT NULL)
             OR (d.id_guia IS NOT NULL AND d.id_hito_actual <> @hito_registrada)
          )
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'RN03: solo las guías registradas y pendientes de despacho pueden asignarse a una hoja de ruta activa.', 1;
    END

    -- RN04: la hoja de ruta debe seguir teniendo conductor y vehículo habilitados.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN deleted d ON d.id_guia = i.id_guia
        JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = i.id_hoja_ruta
        JOIN dbo.usuario   u ON u.id_usuario = h.id_conductor
        JOIN dbo.rol       r ON r.id_rol = u.id_rol
        JOIN dbo.vehiculo  v ON v.id_vehiculo = h.id_vehiculo
        WHERE i.id_hoja_ruta IS NOT NULL
          AND (d.id_guia IS NULL OR ISNULL(d.id_hoja_ruta, 0) <> i.id_hoja_ruta)
          AND (u.activo = 0
               OR r.nombre <> N'Conductor/Operador'
               OR v.estado IN (N'Mantenimiento', N'Inhabilitado'))
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50004, N'RN04: la hoja de ruta tiene conductor o vehículo no habilitado.', 1;
    END

    -- RN04: la carga asignada no puede superar la capacidad del vehículo.
    IF EXISTS (
        SELECT 1
        FROM (
            SELECT i.id_hoja_ruta
            FROM inserted i
            LEFT JOIN deleted d ON d.id_guia = i.id_guia
            WHERE i.id_hoja_ruta IS NOT NULL
              AND (d.id_guia IS NULL
                   OR ISNULL(d.id_hoja_ruta, 0) <> i.id_hoja_ruta
                   OR d.peso_kg <> i.peso_kg
                   OR d.volumen_m3 <> i.volumen_m3)
            GROUP BY i.id_hoja_ruta
        ) q
        JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = q.id_hoja_ruta
        JOIN dbo.vehiculo  v ON v.id_vehiculo = h.id_vehiculo
        JOIN (
            SELECT g.id_hoja_ruta,
                   SUM(g.peso_kg)    AS peso_total,
                   SUM(g.volumen_m3) AS volumen_total
            FROM dbo.guia_envio g
            JOIN dbo.hito hg ON hg.id_hito = g.id_hito_actual
            WHERE g.id_hoja_ruta IS NOT NULL
              AND hg.nombre <> N'Entregado'
            GROUP BY g.id_hoja_ruta
        ) t ON t.id_hoja_ruta = q.id_hoja_ruta
        WHERE t.peso_total > v.capacidad_kg
           OR t.volumen_total > v.capacidad_m3
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50012, N'RN04: la carga de la hoja de ruta supera la capacidad del vehículo.', 1;
    END
END
GO

-- RNF05: una guía entregada queda bloqueada para cualquier modificación.
CREATE TRIGGER dbo.trg_guia_bloqueo_entregada ON dbo.guia_envio
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM deleted d
        JOIN dbo.hito h ON h.id_hito = d.id_hito_actual
        WHERE h.nombre = N'Entregado'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, N'RNF05: una guía entregada no puede modificarse.', 1;
    END
END
GO

-- RN06 / RN13 / RNF05: solo conductor o personal de almacén registran el
-- checkpoint, el conductor solo sobre guías de su hoja de ruta, el hito
-- debe ser el siguiente y la guía no puede estar entregada.
CREATE TRIGGER dbo.trg_checkpoint_valida ON dbo.[checkpoint]
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hito_entregado INT = (SELECT id_hito FROM dbo.hito WHERE nombre = N'Entregado');

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.usuario   u ON u.id_usuario = i.id_usuario
        JOIN dbo.rol       r ON r.id_rol = u.id_rol
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        LEFT JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = g.id_hoja_ruta
        LEFT JOIN dbo.vehiculo  v ON v.id_vehiculo = h.id_vehiculo
        WHERE u.activo = 0
           OR r.nombre NOT IN (N'Conductor/Operador', N'Personal de Almacén')
           OR g.id_hoja_ruta IS NULL
           OR h.id_hoja_ruta IS NULL
           OR h.estado = N'Finalizada'
           OR v.estado IN (N'Mantenimiento', N'Inhabilitado')
           OR (r.nombre = N'Conductor/Operador' AND h.id_conductor <> i.id_usuario)
           OR g.id_hito_actual = @hito_entregado
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50013, N'RN06/RN13: checkpoint no permitido para el usuario, la ruta o el estado de la guía.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        JOIN dbo.hito ha ON ha.id_hito = g.id_hito_actual
        JOIN dbo.hito hn ON hn.id_hito = i.id_hito
        WHERE hn.orden <> ha.orden + 1
           OR hn.nombre = N'Entregado'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, N'RN05: el hito debe ser el siguiente al estado actual de la guía y Entregado solo se registra desde la entrega.', 1;
    END

    UPDATE g
       SET g.id_hito_actual = i.id_hito
    FROM dbo.guia_envio g
    JOIN inserted i ON i.id_guia = g.id_guia;

    -- La hoja de ruta pasa de Pendiente a En curso con el primer checkpoint.
    UPDATE h
       SET h.estado = N'En curso'
    FROM dbo.hoja_ruta h
    JOIN (
        SELECT DISTINCT g.id_hoja_ruta
        FROM inserted i
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        WHERE g.id_hoja_ruta IS NOT NULL
    ) q ON q.id_hoja_ruta = h.id_hoja_ruta
    WHERE h.estado = N'Pendiente';

    -- El vehículo queda En ruta mientras la hoja esté activa.
    UPDATE v
       SET v.estado = N'En ruta'
    FROM dbo.vehiculo v
    JOIN dbo.hoja_ruta h ON h.id_vehiculo = v.id_vehiculo
    JOIN (
        SELECT DISTINCT g.id_hoja_ruta
        FROM inserted i
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        WHERE g.id_hoja_ruta IS NOT NULL
    ) q ON q.id_hoja_ruta = h.id_hoja_ruta
    WHERE v.estado = N'Disponible';
END
GO

-- RN08 / RN13: incidencia documentada, solo por conductor o supervisor,
-- sobre una guía en ruta y no entregada. El conductor solo en su ruta.
CREATE TRIGGER dbo.trg_incidencia_valida ON dbo.incidencia
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @hito_entregado INT = (SELECT id_hito FROM dbo.hito WHERE nombre = N'Entregado');

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.usuario   u ON u.id_usuario = i.id_usuario
        JOIN dbo.rol       r ON r.id_rol = u.id_rol
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        LEFT JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = g.id_hoja_ruta
        WHERE u.activo = 0
           OR r.nombre NOT IN (N'Conductor/Operador', N'Centro de Control/Supervisor')
           OR g.id_hoja_ruta IS NULL
           OR h.id_hoja_ruta IS NULL
           OR h.estado = N'Finalizada'
           OR g.id_hito_actual = @hito_entregado
           OR (r.nombre = N'Conductor/Operador' AND h.id_conductor <> i.id_usuario)
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50014, N'RN08/RN13: la incidencia no es válida para el usuario, la ruta o el estado de la guía.', 1;
    END
END
GO

-- RN09: alerta al Centro de Control y al Administrador Logístico.
CREATE TRIGGER dbo.trg_incidencia_notifica ON dbo.incidencia
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.notificacion (id_usuario, id_guia, tipo, mensaje)
    SELECT u.id_usuario,
           i.id_guia,
           N'Incidencia',
           CONCAT(N'Incidencia "', ti.nombre, N'" reportada en la guía ', g.codigo_guia, N'.')
    FROM inserted i
    JOIN dbo.guia_envio      g  ON g.id_guia = i.id_guia
    JOIN dbo.tipo_incidencia ti ON ti.id_tipo_incidencia = i.id_tipo_incidencia
    JOIN dbo.usuario         u  ON u.activo = 1
    JOIN dbo.rol             r  ON r.id_rol = u.id_rol
    WHERE r.nombre IN (N'Centro de Control/Supervisor', N'Administrador Logístico')
      AND u.id_usuario <> i.id_usuario;
END
GO

-- RN07 / RN13 / RNF05: la entrega exige comprobante, la realiza el conductor
-- de la ruta y solo desde "En Reparto Final". Deja la guía en "Entregado".
CREATE TRIGGER dbo.trg_entrega_valida ON dbo.entrega
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.usuario   u ON u.id_usuario = i.id_usuario
        JOIN dbo.rol       r ON r.id_rol = u.id_rol
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        LEFT JOIN dbo.hoja_ruta h ON h.id_hoja_ruta = g.id_hoja_ruta
        WHERE u.activo = 0
           OR r.nombre <> N'Conductor/Operador'
           OR g.id_hoja_ruta IS NULL
           OR h.id_hoja_ruta IS NULL
           OR h.estado = N'Finalizada'
           OR h.id_conductor <> i.id_usuario
           OR (ISNULL(i.ruta_firma, N'') = N'' AND ISNULL(i.ruta_foto, N'') = N'')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50015, N'RN07/RN13: la entrega no es válida para el usuario, la ruta o el comprobante.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        JOIN dbo.hito h ON h.id_hito = g.id_hito_actual
        WHERE h.nombre <> N'En Reparto Final'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, N'RN07: la guía debe estar en reparto final para confirmar la entrega.', 1;
    END

    UPDATE g
       SET g.id_hito_actual = (SELECT id_hito FROM dbo.hito WHERE nombre = N'Entregado')
    FROM dbo.guia_envio g
    JOIN inserted i ON i.id_guia = g.id_guia;

    -- La hoja se finaliza cuando todas sus guías fueron entregadas.
    UPDATE h
       SET h.estado = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM dbo.guia_envio g2
                            JOIN dbo.hito h2 ON h2.id_hito = g2.id_hito_actual
                            WHERE g2.id_hoja_ruta = h.id_hoja_ruta
                              AND h2.nombre <> N'Entregado'
                        ) THEN N'En curso'
                        ELSE N'Finalizada'
                      END
    FROM dbo.hoja_ruta h
    JOIN (
        SELECT DISTINCT g.id_hoja_ruta
        FROM inserted i
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        WHERE g.id_hoja_ruta IS NOT NULL
    ) q ON q.id_hoja_ruta = h.id_hoja_ruta
    WHERE h.estado <> N'Finalizada';

    UPDATE v
       SET v.estado = N'Disponible'
    FROM dbo.vehiculo v
    JOIN dbo.hoja_ruta h ON h.id_vehiculo = v.id_vehiculo
    JOIN (
        SELECT DISTINCT g.id_hoja_ruta
        FROM inserted i
        JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
        WHERE g.id_hoja_ruta IS NOT NULL
    ) q ON q.id_hoja_ruta = h.id_hoja_ruta
    WHERE h.estado = N'Finalizada'
      AND v.estado = N'En ruta';
END
GO

-- RN09: notifica al Administrador Logístico cuando finaliza una entrega.
CREATE TRIGGER dbo.trg_entrega_notifica ON dbo.entrega
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.notificacion (id_usuario, id_guia, tipo, mensaje)
    SELECT u.id_usuario,
           i.id_guia,
           N'Entrega finalizada',
           CONCAT(N'La guía ', g.codigo_guia, N' fue entregada a ', g.destinatario, N'.')
    FROM inserted i
    JOIN dbo.guia_envio g ON g.id_guia = i.id_guia
    JOIN dbo.usuario    u ON u.activo = 1
    JOIN dbo.rol        r ON r.id_rol = u.id_rol
    WHERE r.nombre = N'Administrador Logístico';
END
GO

/* ---------- RNF05: eventos de trazabilidad inmutables ---------- */
CREATE TRIGGER dbo.trg_checkpoint_no_delete ON dbo.[checkpoint]
INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50005, N'RNF05: los checkpoints no pueden eliminarse.', 1;
END
GO

CREATE TRIGGER dbo.trg_checkpoint_no_update ON dbo.[checkpoint]
INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50017, N'RNF05: los checkpoints no pueden modificarse.', 1;
END
GO

CREATE TRIGGER dbo.trg_incidencia_no_delete ON dbo.incidencia
INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50006, N'RNF05: las incidencias no pueden eliminarse.', 1;
END
GO

CREATE TRIGGER dbo.trg_incidencia_no_update ON dbo.incidencia
INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50017, N'RNF05: las incidencias no pueden modificarse.', 1;
END
GO

CREATE TRIGGER dbo.trg_entrega_no_delete ON dbo.entrega
INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50007, N'RNF05: las entregas no pueden eliminarse.', 1;
END
GO

CREATE TRIGGER dbo.trg_entrega_no_update ON dbo.entrega
INSTEAD OF UPDATE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50017, N'RNF05: las entregas no pueden modificarse.', 1;
END
GO

-- Los movimientos de inventario son un libro auditable: no se editan ni borran.
CREATE TRIGGER dbo.trg_movimiento_inmutable ON dbo.movimiento_inventario
INSTEAD OF UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50020, N'RNF05: los movimientos de inventario no pueden modificarse ni eliminarse.', 1;
END
GO

-- RNF05: el detalle de una guía ya despachada o entregada queda bloqueado.
CREATE TRIGGER dbo.trg_detalle_guia_bloqueo ON dbo.detalle_guia
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM (
            SELECT id_guia FROM inserted
            UNION
            SELECT id_guia FROM deleted
        ) x
        JOIN dbo.guia_envio g ON g.id_guia = x.id_guia
        JOIN dbo.hito h ON h.id_hito = g.id_hito_actual
        WHERE h.nombre = N'Entregado'
           OR EXISTS (SELECT 1 FROM dbo.[checkpoint] c WHERE c.id_guia = x.id_guia)
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50025, N'RNF05: el detalle de una guía despachada o entregada no puede modificarse.', 1;
    END
END
GO

-- RN11: los vehículos se inhabilitan, no se eliminan.
CREATE TRIGGER dbo.trg_vehiculo_no_delete ON dbo.vehiculo
INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 50010, N'RN11: los vehículos no se eliminan; deben inhabilitarse (estado = Inhabilitado).', 1;
END
GO

-- El estado del vehículo no puede salir de "En ruta" mientras tenga una hoja activa.
CREATE TRIGGER dbo.trg_vehiculo_estado_valida ON dbo.vehiculo
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.id_vehiculo = i.id_vehiculo
        WHERE i.estado <> d.estado
          AND i.estado IN (N'Disponible', N'Mantenimiento', N'Inhabilitado')
          AND EXISTS (
              SELECT 1 FROM dbo.hoja_ruta h
              WHERE h.id_vehiculo = i.id_vehiculo
                AND h.estado IN (N'Pendiente', N'En curso')
          )
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50021, N'RN04: el vehículo tiene una hoja de ruta activa; no puede cambiar a Disponible, Mantenimiento o Inhabilitado.', 1;
    END
END
GO

-- Actualización automática del stock: el libro de movimientos es la fuente.
CREATE TRIGGER dbo.trg_movimiento_inventario ON dbo.movimiento_inventario
AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.usuario u ON u.id_usuario = i.id_usuario
        WHERE u.activo = 0
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50016, N'El usuario que registra el movimiento está inhabilitado.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM (
            SELECT id_almacen,
                   id_producto,
                   SUM(CASE WHEN tipo_movimiento = N'ENTRADA' THEN cantidad ELSE -cantidad END) AS delta
            FROM inserted
            GROUP BY id_almacen, id_producto
        ) m
        LEFT JOIN dbo.inventario v
               ON v.id_almacen = m.id_almacen AND v.id_producto = m.id_producto
        WHERE ISNULL(v.stock_actual, 0) + m.delta < 0
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50016, N'Inventario insuficiente para registrar la salida.', 1;
    END

    ;WITH mov AS (
        SELECT id_almacen,
               id_producto,
               SUM(CASE WHEN tipo_movimiento = N'ENTRADA' THEN cantidad ELSE -cantidad END) AS delta
        FROM inserted
        GROUP BY id_almacen, id_producto
    )
    INSERT INTO dbo.inventario (id_almacen, id_producto, stock_actual)
    SELECT m.id_almacen, m.id_producto, 0
    FROM mov m
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.inventario v
        WHERE v.id_almacen = m.id_almacen AND v.id_producto = m.id_producto
    );

    ;WITH mov AS (
        SELECT id_almacen,
               id_producto,
               SUM(CASE WHEN tipo_movimiento = N'ENTRADA' THEN cantidad ELSE -cantidad END) AS delta
        FROM inserted
        GROUP BY id_almacen, id_producto
    )
    UPDATE v
       SET v.stock_actual = v.stock_actual + m.delta
    FROM dbo.inventario v
    JOIN mov m ON m.id_almacen = v.id_almacen AND m.id_producto = v.id_producto;
END
GO

/* ============================================================
   8. DATOS INICIALES Y DEMOSTRACIÓN
   ------------------------------------------------------------
   Contraseña de todos los usuarios de prueba: LogiTrack2026!
   (hashes BCrypt reales, factor 11)
   ============================================================ */
INSERT INTO dbo.rol (nombre) VALUES
 (N'Administrador Logístico'),
 (N'Administrador del Sistema'),
 (N'Conductor/Operador'),
 (N'Personal de Almacén'),
 (N'Centro de Control/Supervisor');

-- Los ids de usuario 1..7 se asignan en este orden. Ana = 1 (Admin. Logístico),
-- Luis = 2 (Admin. Sistema), Carlos = 3 (Conductor), Marta = 4 (Almacén),
-- Pedro = 5 (Centro de Control), Diego = 6 (Conductor), Sofía = 7 (Conductor inactiva).
INSERT INTO dbo.hito (nombre, orden) VALUES
 (N'Registrada', 0),
 (N'Salida de Almacén', 1),
 (N'En Tránsito', 2),
 (N'En Centro de Distribución Local', 3),
 (N'En Reparto Final', 4),
 (N'Entregado', 5);

INSERT INTO dbo.tipo_carga (nombre) VALUES
 (N'General'), (N'Frágil'), (N'Refrigerada'), (N'Peligrosa');

INSERT INTO dbo.tipo_incidencia (nombre) VALUES
 (N'Cierre vial'), (N'Falla mecánica'), (N'Condiciones climáticas adversas'), (N'Otro');

INSERT INTO dbo.usuario (id_rol, nombre_completo, correo, password_hash, activo) VALUES
 (1, N'Ana Rojas',    N'ana.rojas@logitrack.test',    N'$2a$11$tkF6GK2fVa47zb.n37Lom.3cRKew4U4qsvzLYEqSA6WVU/UuKGBiC', 1),
 (2, N'Luis Mendez',  N'luis.mendez@logitrack.test',  N'$2a$11$pN/w8lbOPi52HRAgandNZe3jTy/vSh4sAyc6OzLTzSa9z6ezv4m8K', 1),
 (3, N'Carlos Soliz', N'carlos.soliz@logitrack.test', N'$2a$11$I2eQzbbXMPaiG9KpqpVpOOC0c9OXutiRmcUvW5gM8XVOWO1SZzoWm', 1),
 (4, N'Marta Vaca',   N'marta.vaca@logitrack.test',   N'$2a$11$tUFsUbJe8kcyfBqS5Yy8HOZvRY1DYYtDSyzf.pWWwpy1KGJh.UReq', 1),
 (5, N'Pedro Egüez',  N'pedro.eguez@logitrack.test',  N'$2a$11$GdjDbf2.5w/YII0S4ITUJO/fZjO5TwSVZ5d1iDbRha69g6j90L/B6', 1),
 (3, N'Diego Vargas', N'diego.vargas@logitrack.test', N'$2a$11$/R28XE3Vbe0.fheyRNs1/ukcHyTmlC3o.9nGnqLQzNgTvP.g.P0I.', 1),
 (3, N'Sofía Campos', N'sofia.campos@logitrack.test', N'$2a$11$/R28XE3Vbe0.fheyRNs1/ukcHyTmlC3o.9nGnqLQzNgTvP.g.P0I.', 0);

INSERT INTO dbo.almacen (nombre, tipo, ciudad, direccion) VALUES
 (N'Almacén Central',     N'Almacén',               N'Santa Cruz de la Sierra', N'Parque Industrial, Av. 3er Anillo'),
 (N'CD Local Norte',      N'Centro de Distribución', N'Santa Cruz de la Sierra', N'Zona Norte, Av. Banzer km 8'),
 (N'CD Local Sur',        N'Centro de Distribución', N'Santa Cruz de la Sierra', N'Zona Sur, Av. Santos Dumont');

INSERT INTO dbo.cliente (razon_social, nit, telefono, direccion) VALUES
 (N'Comercial Andina S.R.L.',       N'1020304050', N'70000001', N'Av. Banzer km 5'),
 (N'Distribuidora El Oriente S.A.', N'1020304051', N'70000002', N'Av. Cristo Redentor km 3'),
 (N'Ferretería Central S.R.L.',     N'1020304052', N'70000003', N'Calle Sucre 120');

INSERT INTO dbo.producto (sku, nombre, peso_unitario_kg, volumen_unitario_m3) VALUES
 (N'SKU-0001', N'Caja de herramientas', 2.500, 0.0300),
 (N'SKU-0002', N'Bobina de cable',      8.000, 0.0500),
 (N'SKU-0003', N'Caja de tornillos',    1.200, 0.0080),
 (N'SKU-0004', N'Reflector LED',        0.800, 0.0040);

INSERT INTO dbo.inventario (id_almacen, id_producto, stock_actual) VALUES
 (1, 1, 200), (1, 2, 80), (1, 3, 500), (1, 4, 120),
 (2, 1,  40), (2, 2, 30), (2, 3, 100), (2, 4,  60);

INSERT INTO dbo.vehiculo (placa, tipo, capacidad_kg, capacidad_m3, estado) VALUES
 (N'1234-ABC', N'Camión 3 t',   3000.00, 18.00, N'Disponible'),
 (N'5678-DEF', N'Furgón 1.5 t', 1500.00, 10.00, N'Disponible'),
 (N'9012-GHI', N'Camión 8 t',   8000.00, 40.00, N'Disponible');

/* ---------- Flujo de demostración ---------- */
DECLARE @ahora DATETIME2(0) = SYSDATETIME();

-- Hoja 1: Carlos (conductor 3) con camión 1234-ABC.
-- Hoja 2: Diego (conductor 6) con furgón 5678-DEF.
INSERT INTO dbo.hoja_ruta (id_vehiculo, id_conductor, id_usuario_creador, estado, fecha_creacion) VALUES
 (1, 3, 1, N'Pendiente', DATEADD(HOUR, -49, @ahora)),
 (2, 6, 1, N'Pendiente', DATEADD(HOUR, -24, @ahora));

INSERT INTO dbo.guia_envio
 (id_cliente, id_almacen_origen, id_tipo_carga, id_hoja_ruta, id_usuario_registro,
  destinatario, direccion_destino, peso_kg, volumen_m3, fecha_registro)
VALUES
 (1, 1, 1, 1,    1, N'Juan Pérez',      N'Calle 21 de Mayo 100',      25.00, 0.300, DATEADD(HOUR, -50, @ahora)),
 (2, 1, 2, 1,    1, N'María Gutiérrez', N'Av. Cristo Redentor 250',   16.00, 0.200, DATEADD(HOUR, -48, @ahora)),
 (3, 1, 3, 2,    1, N'Pedro Salazar',   N'Av. Santos Dumont 300',     40.00, 0.500, DATEADD(HOUR, -23, @ahora)),
 (1, 1, 1, NULL, 1, N'Lucía Fernández', N'Calle Sucre 45',            12.00, 0.150, DATEADD(HOUR,  -2, @ahora));

INSERT INTO dbo.detalle_guia (id_guia, id_producto, cantidad) VALUES
 (1, 1, 8), (1, 3, 4),
 (2, 2, 2),
 (3, 3, 20), (3, 4, 10),
 (4, 4, 5);

-- El stock se actualiza automáticamente por trg_movimiento_inventario.
INSERT INTO dbo.movimiento_inventario
 (id_almacen, id_producto, id_guia, id_usuario, tipo_movimiento, cantidad, fecha_hora)
VALUES
 (1, 1, 1,    4, N'SALIDA',   8, DATEADD(HOUR, -49, @ahora)),
 (1, 3, 1,    4, N'SALIDA',   4, DATEADD(HOUR, -49, @ahora)),
 (1, 2, 2,    4, N'SALIDA',   2, DATEADD(HOUR, -47, @ahora)),
 (1, 3, 3,    4, N'SALIDA',  20, DATEADD(HOUR, -22, @ahora)),
 (1, 4, 3,    4, N'SALIDA',  10, DATEADD(HOUR, -22, @ahora)),
 (1, 1, NULL, 4, N'ENTRADA', 50, DATEADD(HOUR, -12, @ahora));

/* Guía 1: recorre todos los hitos y termina entregada. */
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (1, 2, 1, 4, DATEADD(HOUR, -47, @ahora));
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (1, 3, NULL, 3, DATEADD(HOUR, -40, @ahora));
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (1, 4, 2, 4, DATEADD(HOUR, -20, @ahora));
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (1, 5, NULL, 3, DATEADD(HOUR, -6, @ahora));

/* Guía 2: queda En Tránsito y con una incidencia. */
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (2, 2, 1, 4, DATEADD(HOUR, -46, @ahora));
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (2, 3, NULL, 3, DATEADD(HOUR, -38, @ahora));

/* Guía 3: recién salió de almacén. */
INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario, fecha_hora)
VALUES (3, 2, 1, 4, DATEADD(HOUR, -20, @ahora));

INSERT INTO dbo.incidencia
 (id_guia, id_tipo_incidencia, id_usuario, descripcion, ruta_foto, fecha_hora)
VALUES
 (2, 1, 3, N'Cierre vial en la ruta al centro; se desvía el recorrido.',
          N'/evidencias/INC-0001.jpg', DATEADD(HOUR, -36, @ahora));

INSERT INTO dbo.entrega
 (id_guia, id_usuario, nombre_receptor, ruta_firma, ruta_foto, fecha_hora)
VALUES
 (1, 3, N'Juan Pérez', N'/firmas/LT-00000001.png', NULL, DATEADD(HOUR, -4, @ahora));

/* ============================================================
   9. VERIFICACIÓN RÁPIDA (ejecutar después del script)
   ============================================================ */
SELECT 'roles' AS entidad, COUNT(*) AS total FROM dbo.rol
UNION ALL SELECT 'usuarios',     COUNT(*) FROM dbo.usuario
UNION ALL SELECT 'almacenes',    COUNT(*) FROM dbo.almacen
UNION ALL SELECT 'clientes',     COUNT(*) FROM dbo.cliente
UNION ALL SELECT 'productos',    COUNT(*) FROM dbo.producto
UNION ALL SELECT 'vehiculos',    COUNT(*) FROM dbo.vehiculo
UNION ALL SELECT 'hojas_ruta',   COUNT(*) FROM dbo.hoja_ruta
UNION ALL SELECT 'guias',        COUNT(*) FROM dbo.guia_envio
UNION ALL SELECT 'checkpoints',  COUNT(*) FROM dbo.[checkpoint]
UNION ALL SELECT 'incidencias',  COUNT(*) FROM dbo.incidencia
UNION ALL SELECT 'entregas',     COUNT(*) FROM dbo.entrega
UNION ALL SELECT 'notificacion', COUNT(*) FROM dbo.notificacion;

SELECT * FROM dbo.inventario ORDER BY id_almacen, id_producto;

SELECT * FROM dbo.vw_reporte_envios ORDER BY fecha_registro;

SELECT codigo_guia, estado_actual, tipo_evento, hito, orden_hito, fecha_hora, estacion
FROM dbo.vw_trazabilidad_publica
WHERE codigo_guia = N'LT-00000001'
ORDER BY orden_hito, fecha_hora;

/* ============================================================
   10. PRUEBAS DE REGLAS (ejecutar manualmente; deben fallar)
   ------------------------------------------------------------
   BEGIN TRAN; ... ROLLBACK;  para no dejar cambios.
   ============================================================ */
-- RN05: checkpoint fuera de orden (guía 4 está en Registrada, no en En Tránsito)
-- INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario)
-- VALUES (4, 3, NULL, 3);                       -- error 50013 (guía sin ruta) o 50001

-- RN05: omitir un hito (guía 2 está En Tránsito, no en Reparto Final)
-- INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario)
-- VALUES (2, 5, NULL, 3);                       -- error 50001

-- RNF05: modificar una guía entregada
-- UPDATE dbo.guia_envio SET destinatario = N'Otro' WHERE id_guia = 1;  -- error 50003

-- RNF05: eliminar un checkpoint
-- DELETE FROM dbo.[checkpoint] WHERE id_guia = 1;  -- error 50005

-- RN07: entrega sin comprobante
-- INSERT INTO dbo.entrega (id_guia, id_usuario, nombre_receptor)
-- VALUES (3, 6, N'Pedro Salazar');              -- error de CHECK CK_entrega_comprobante

-- RN13: conductor 6 intenta registrar checkpoint en una guía de la ruta 1
-- INSERT INTO dbo.[checkpoint] (id_guia, id_hito, id_almacen, id_usuario)
-- VALUES (2, 4, 2, 6);                          -- error 50013

-- RN11: eliminar un usuario
-- DELETE FROM dbo.usuario WHERE id_usuario = 7; -- error 50010

-- RN04: inhabilitar al conductor 3, que tiene la hoja de ruta 1 activa
-- UPDATE dbo.usuario SET activo = 0 WHERE id_usuario = 3; -- error 50024

-- RNF05: alterar el detalle de una guía ya entregada
-- DELETE FROM dbo.detalle_guia WHERE id_guia = 1 AND id_producto = 1; -- error 50025

-- RN12: un Administrador del Sistema intenta registrar una guía
-- INSERT INTO dbo.guia_envio (id_cliente, id_almacen_origen, id_tipo_carga,
--     id_usuario_registro, destinatario, direccion_destino, peso_kg, volumen_m3)
-- VALUES (1, 1, 1, 2, N'Prueba', N'Destino', 1, 0.1); -- error 50018

-- Inventario insuficiente
-- INSERT INTO dbo.movimiento_inventario (id_almacen, id_producto, id_usuario,
--     tipo_movimiento, cantidad)
-- VALUES (1, 1, 4, N'SALIDA', 999999);          -- error 50016
