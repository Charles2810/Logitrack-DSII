using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Shared.Entities;

[Table("rol")]
public class Rol
{
    [Key, Column("id_rol")]
    public int IdRol { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

[Table("usuario")]
public class Usuario
{
    [Key, Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("id_rol")]
    public int IdRol { get; set; }

    [Column("nombre_completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Column("correo")]
    public string Correo { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    public Rol? Rol { get; set; }
}

[Table("cliente")]
public class Cliente
{
    [Key, Column("id_cliente")]
    public int IdCliente { get; set; }

    [Column("razon_social")]
    public string RazonSocial { get; set; } = string.Empty;

    [Column("nit")]
    public string Nit { get; set; } = string.Empty;

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("direccion")]
    public string? Direccion { get; set; }
}

[Table("almacen")]
public class Almacen
{
    [Key, Column("id_almacen")]
    public int IdAlmacen { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("ciudad")]
    public string Ciudad { get; set; } = string.Empty;

    [Column("direccion")]
    public string? Direccion { get; set; }
}

[Table("tipo_carga")]
public class TipoCarga
{
    [Key, Column("id_tipo_carga")]
    public int IdTipoCarga { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

[Table("hito")]
public class Hito
{
    [Key, Column("id_hito")]
    public int IdHito { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("orden")]
    public byte Orden { get; set; }
}

[Table("tipo_incidencia")]
public class TipoIncidencia
{
    [Key, Column("id_tipo_incidencia")]
    public int IdTipoIncidencia { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

[Table("vehiculo")]
public class Vehiculo
{
    [Key, Column("id_vehiculo")]
    public int IdVehiculo { get; set; }

    [Column("placa")]
    public string Placa { get; set; } = string.Empty;

    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("capacidad_kg")]
    public decimal CapacidadKg { get; set; }

    [Column("capacidad_m3")]
    public decimal CapacidadM3 { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = string.Empty;
}

[Table("producto")]
public class Producto
{
    [Key, Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("peso_unitario_kg")]
    public decimal PesoUnitarioKg { get; set; }

    [Column("volumen_unitario_m3")]
    public decimal VolumenUnitarioM3 { get; set; }
}

[Table("inventario")]
public class Inventario
{
    [Column("id_almacen")]
    public int IdAlmacen { get; set; }

    [Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("stock_actual")]
    public int StockActual { get; set; }

    public Almacen? Almacen { get; set; }
    public Producto? Producto { get; set; }
}

[Table("hoja_ruta")]
public class HojaRuta
{
    [Key, Column("id_hoja_ruta")]
    public int IdHojaRuta { get; set; }

    [Column("id_vehiculo")]
    public int IdVehiculo { get; set; }

    [Column("id_conductor")]
    public int IdConductor { get; set; }

    [Column("id_usuario_creador")]
    public int IdUsuarioCreador { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = string.Empty;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }

    public Vehiculo? Vehiculo { get; set; }
    public Usuario? Conductor { get; set; }
    public Usuario? UsuarioCreador { get; set; }
    public ICollection<GuiaEnvio> Guias { get; set; } = new List<GuiaEnvio>();
}

[Table("guia_envio")]
public class GuiaEnvio
{
    [Key, Column("id_guia")]
    public int IdGuia { get; set; }

    [Column("codigo_guia")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string CodigoGuia { get; set; } = string.Empty;

    [Column("id_cliente")]
    public int IdCliente { get; set; }

    [Column("id_almacen_origen")]
    public int IdAlmacenOrigen { get; set; }

    [Column("id_tipo_carga")]
    public int IdTipoCarga { get; set; }

    [Column("id_hito_actual")]
    public int IdHitoActual { get; set; }

    [Column("id_hoja_ruta")]
    public int? IdHojaRuta { get; set; }

    [Column("id_usuario_registro")]
    public int IdUsuarioRegistro { get; set; }

    [Column("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [Column("direccion_destino")]
    public string DireccionDestino { get; set; } = string.Empty;

    [Column("peso_kg")]
    public decimal PesoKg { get; set; }

    [Column("volumen_m3")]
    public decimal VolumenM3 { get; set; }

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; }

    public Cliente? Cliente { get; set; }
    public Almacen? AlmacenOrigen { get; set; }
    public TipoCarga? TipoCarga { get; set; }
    public Hito? HitoActual { get; set; }
    public HojaRuta? HojaRuta { get; set; }
    public Usuario? UsuarioRegistro { get; set; }
    public ICollection<DetalleGuia> Detalles { get; set; } = new List<DetalleGuia>();
    public ICollection<Checkpoint> Checkpoints { get; set; } = new List<Checkpoint>();
    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();
    public Entrega? Entrega { get; set; }
}

[Table("detalle_guia")]
public class DetalleGuia
{
    [Column("id_guia")]
    public int IdGuia { get; set; }

    [Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("cantidad")]
    public int Cantidad { get; set; }

    public GuiaEnvio? Guia { get; set; }
    public Producto? Producto { get; set; }
}

[Table("movimiento_inventario")]
public class MovimientoInventario
{
    [Key, Column("id_movimiento")]
    public int IdMovimiento { get; set; }

    [Column("id_almacen")]
    public int IdAlmacen { get; set; }

    [Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("id_guia")]
    public int? IdGuia { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("tipo_movimiento")]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    public Almacen? Almacen { get; set; }
    public Producto? Producto { get; set; }
    public GuiaEnvio? Guia { get; set; }
    public Usuario? Usuario { get; set; }
}

[Table("checkpoint")]
public class Checkpoint
{
    [Key, Column("id_checkpoint")]
    public int IdCheckpoint { get; set; }

    [Column("id_guia")]
    public int IdGuia { get; set; }

    [Column("id_hito")]
    public int IdHito { get; set; }

    [Column("id_almacen")]
    public int? IdAlmacen { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    public GuiaEnvio? Guia { get; set; }
    public Hito? Hito { get; set; }
    public Almacen? Almacen { get; set; }
    public Usuario? Usuario { get; set; }
}

[Table("incidencia")]
public class Incidencia
{
    [Key, Column("id_incidencia")]
    public int IdIncidencia { get; set; }

    [Column("id_guia")]
    public int IdGuia { get; set; }

    [Column("id_tipo_incidencia")]
    public int IdTipoIncidencia { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Column("ruta_foto")]
    public string RutaFoto { get; set; } = string.Empty;

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    public GuiaEnvio? Guia { get; set; }
    public TipoIncidencia? TipoIncidencia { get; set; }
    public Usuario? Usuario { get; set; }
}

[Table("entrega")]
public class Entrega
{
    [Key, Column("id_entrega")]
    public int IdEntrega { get; set; }

    [Column("id_guia")]
    public int IdGuia { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("nombre_receptor")]
    public string? NombreReceptor { get; set; }

    [Column("ruta_firma")]
    public string? RutaFirma { get; set; }

    [Column("ruta_foto")]
    public string? RutaFoto { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    public GuiaEnvio? Guia { get; set; }
    public Usuario? Usuario { get; set; }
}

[Table("notificacion")]
public class Notificacion
{
    [Key, Column("id_notificacion")]
    public int IdNotificacion { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("id_guia")]
    public int? IdGuia { get; set; }

    [Column("id_hoja_ruta")]
    public int? IdHojaRuta { get; set; }

    [Column("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Column("mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [Column("leida")]
    public bool Leida { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    public Usuario? Usuario { get; set; }
    public GuiaEnvio? Guia { get; set; }
    public HojaRuta? HojaRuta { get; set; }
}

// Las vistas se mapean como entidades sin clave en AppDbContext.
public class TrazabilidadPublicaView
{
    public string CodigoGuia { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;
    public string TipoEvento { get; set; } = string.Empty;
    public string Hito { get; set; } = string.Empty;
    public byte OrdenHito { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Estacion { get; set; }
}

public class ReporteEnvioView
{
    public int IdGuia { get; set; }
    public string CodigoGuia { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string AlmacenOrigen { get; set; } = string.Empty;
    public string TipoCarga { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string DireccionDestino { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public decimal VolumenM3 { get; set; }
    public string EstadoActual { get; set; } = string.Empty;
    public string? EstadoHojaRuta { get; set; }
    public string? Vehiculo { get; set; }
    public string? Conductor { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public decimal? HorasTransito { get; set; }
    public int TotalIncidencias { get; set; }
}
