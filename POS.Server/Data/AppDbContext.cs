using Microsoft.EntityFrameworkCore;
using POS.Shared.Entities;

namespace POS.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Seguridad
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    // Catálogos y maestros
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<TipoCarga> TiposCarga => Set<TipoCarga>();
    public DbSet<Hito> Hitos => Set<Hito>();
    public DbSet<TipoIncidencia> TiposIncidencia => Set<TipoIncidencia>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    // Inventario
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    // Distribución y trazabilidad
    public DbSet<HojaRuta> HojasRuta => Set<HojaRuta>();
    public DbSet<GuiaEnvio> GuiasEnvio => Set<GuiaEnvio>();
    public DbSet<DetalleGuia> DetallesGuia => Set<DetalleGuia>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<Entrega> Entregas => Set<Entrega>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    // Vistas
    public DbSet<TrazabilidadPublicaView> TrazabilidadPublica => Set<TrazabilidadPublicaView>();
    public DbSet<ReporteEnvioView> ReporteEnvios => Set<ReporteEnvioView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("rol", "dbo");
            e.HasKey(x => x.IdRol);
            e.Property(x => x.IdRol).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuario", "dbo", tb => tb.HasTrigger("trg_usuario_estado_valida"));
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).ValueGeneratedOnAdd();
            e.Property(x => x.NombreCompleto).HasMaxLength(120).IsRequired();
            e.Property(x => x.Correo).HasMaxLength(120).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Activo).HasDefaultValue(true);
            e.Property(x => x.FechaCreacion).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.Correo).IsUnique();
            e.HasOne(x => x.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(x => x.IdRol)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("cliente", "dbo");
            e.HasKey(x => x.IdCliente);
            e.Property(x => x.IdCliente).ValueGeneratedOnAdd();
            e.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
            e.Property(x => x.Nit).HasMaxLength(30).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(25);
            e.Property(x => x.Direccion).HasMaxLength(200);
            e.Property(x => x.Latitud).HasPrecision(9, 6);
            e.Property(x => x.Longitud).HasPrecision(9, 6);
            e.HasIndex(x => x.Nit).IsUnique();
        });

        modelBuilder.Entity<Almacen>(e =>
        {
            e.ToTable("almacen", "dbo");
            e.HasKey(x => x.IdAlmacen);
            e.Property(x => x.IdAlmacen).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Tipo).HasMaxLength(30).IsRequired();
            e.Property(x => x.Ciudad).HasMaxLength(80).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(200);
            e.Property(x => x.Latitud).HasPrecision(9, 6);
            e.Property(x => x.Longitud).HasPrecision(9, 6);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<TipoCarga>(e =>
        {
            e.ToTable("tipo_carga", "dbo");
            e.HasKey(x => x.IdTipoCarga);
            e.Property(x => x.IdTipoCarga).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Hito>(e =>
        {
            e.ToTable("hito", "dbo");
            e.HasKey(x => x.IdHito);
            e.Property(x => x.IdHito).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
            e.Property(x => x.Orden).HasColumnType("tinyint");
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasIndex(x => x.Orden).IsUnique();
        });

        modelBuilder.Entity<TipoIncidencia>(e =>
        {
            e.ToTable("tipo_incidencia", "dbo");
            e.HasKey(x => x.IdTipoIncidencia);
            e.Property(x => x.IdTipoIncidencia).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Vehiculo>(e =>
        {
            e.ToTable("vehiculo", "dbo", tb => tb.HasTrigger("trg_vehiculo_estado_valida"));
            e.HasKey(x => x.IdVehiculo);
            e.Property(x => x.IdVehiculo).ValueGeneratedOnAdd();
            e.Property(x => x.Placa).HasMaxLength(15).IsRequired();
            e.Property(x => x.Tipo).HasMaxLength(40).IsRequired();
            e.Property(x => x.CapacidadKg).HasPrecision(10, 2);
            e.Property(x => x.CapacidadM3).HasPrecision(10, 2);
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Disponible");
            e.HasIndex(x => x.Placa).IsUnique();
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.ToTable("producto", "dbo");
            e.HasKey(x => x.IdProducto);
            e.Property(x => x.IdProducto).ValueGeneratedOnAdd();
            e.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.PesoUnitarioKg).HasPrecision(10, 3);
            e.Property(x => x.VolumenUnitarioM3).HasPrecision(10, 4);
            e.HasIndex(x => x.Sku).IsUnique();
        });

        modelBuilder.Entity<Inventario>(e =>
        {
            e.ToTable("inventario", "dbo");
            e.HasKey(x => new { x.IdAlmacen, x.IdProducto });
            e.Property(x => x.StockActual).HasDefaultValue(0);
            e.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.IdAlmacen)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<HojaRuta>(e =>
        {
            e.ToTable("hoja_ruta", "dbo", tb =>
            {
                tb.HasTrigger("trg_hoja_ruta_valida");
                tb.HasTrigger("trg_hoja_ruta_notifica");
            });
            e.HasKey(x => x.IdHojaRuta);
            e.Property(x => x.IdHojaRuta).ValueGeneratedOnAdd();
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("Pendiente");
            e.Property(x => x.FechaCreacion).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Vehiculo)
                .WithMany()
                .HasForeignKey(x => x.IdVehiculo)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Conductor)
                .WithMany()
                .HasForeignKey(x => x.IdConductor)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.UsuarioCreador)
                .WithMany()
                .HasForeignKey(x => x.IdUsuarioCreador)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<GuiaEnvio>(e =>
        {
            e.ToTable("guia_envio", "dbo", tb =>
            {
                tb.HasTrigger("trg_guia_envio_valida");
                tb.HasTrigger("trg_guia_bloqueo_entregada");
                tb.HasTrigger("trg_guia_envio_asigna_notifica");
            });
            e.HasKey(x => x.IdGuia);
            e.Property(x => x.IdGuia).ValueGeneratedOnAdd();
            e.Property(x => x.CodigoGuia).HasMaxLength(11).ValueGeneratedOnAddOrUpdate();
            e.Property(x => x.IdHitoActual).HasDefaultValue(1);
            e.Property(x => x.Destinatario).HasMaxLength(120).IsRequired();
            e.Property(x => x.DireccionDestino).HasMaxLength(200).IsRequired();
            e.Property(x => x.LatitudDestino).HasPrecision(9, 6);
            e.Property(x => x.LongitudDestino).HasPrecision(9, 6);
            e.Property(x => x.DistanciaEstimadaKm).HasPrecision(10, 2);
            e.Property(x => x.PesoKg).HasPrecision(10, 2);
            e.Property(x => x.VolumenM3).HasPrecision(10, 3);
            e.Property(x => x.FechaRegistro).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.CodigoGuia).IsUnique();
            e.HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.IdCliente)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.AlmacenOrigen)
                .WithMany()
                .HasForeignKey(x => x.IdAlmacenOrigen)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.TipoCarga)
                .WithMany()
                .HasForeignKey(x => x.IdTipoCarga)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.HitoActual)
                .WithMany()
                .HasForeignKey(x => x.IdHitoActual)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.HojaRuta)
                .WithMany(h => h.Guias)
                .HasForeignKey(x => x.IdHojaRuta)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.UsuarioRegistro)
                .WithMany()
                .HasForeignKey(x => x.IdUsuarioRegistro)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Detalles)
                .WithOne(d => d.Guia)
                .HasForeignKey(d => d.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Checkpoints)
                .WithOne(c => c.Guia)
                .HasForeignKey(c => c.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasMany(x => x.Incidencias)
                .WithOne(i => i.Guia)
                .HasForeignKey(i => i.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Entrega)
                .WithOne(en => en.Guia)
                .HasForeignKey<Entrega>(en => en.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DetalleGuia>(e =>
        {
            e.ToTable("detalle_guia", "dbo", tb => tb.HasTrigger("trg_detalle_guia_bloqueo"));
            e.HasKey(x => new { x.IdGuia, x.IdProducto });
            e.HasOne(x => x.Guia)
                .WithMany(g => g.Detalles)
                .HasForeignKey(x => x.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("movimiento_inventario", "dbo", tb => tb.HasTrigger("trg_movimiento_inventario"));
            e.HasKey(x => x.IdMovimiento);
            e.Property(x => x.IdMovimiento).ValueGeneratedOnAdd();
            e.Property(x => x.TipoMovimiento).HasMaxLength(10).IsRequired();
            e.Property(x => x.FechaHora).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.IdAlmacen)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Guia)
                .WithMany()
                .HasForeignKey(x => x.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Checkpoint>(e =>
        {
            e.ToTable("checkpoint", "dbo", tb => tb.HasTrigger("trg_checkpoint_valida"));
            e.HasKey(x => x.IdCheckpoint);
            e.Property(x => x.IdCheckpoint).ValueGeneratedOnAdd();
            e.Property(x => x.FechaHora).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => new { x.IdGuia, x.IdHito }).IsUnique();
            e.HasOne(x => x.Guia)
                .WithMany(g => g.Checkpoints)
                .HasForeignKey(x => x.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Hito)
                .WithMany()
                .HasForeignKey(x => x.IdHito)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Almacen)
                .WithMany()
                .HasForeignKey(x => x.IdAlmacen)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Incidencia>(e =>
        {
            e.ToTable("incidencia", "dbo", tb => tb.HasTrigger("trg_incidencia_valida"));
            e.HasKey(x => x.IdIncidencia);
            e.Property(x => x.IdIncidencia).ValueGeneratedOnAdd();
            e.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
            e.Property(x => x.RutaFoto).HasMaxLength(300).IsRequired();
            e.Property(x => x.FechaHora).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Guia)
                .WithMany(g => g.Incidencias)
                .HasForeignKey(x => x.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.TipoIncidencia)
                .WithMany()
                .HasForeignKey(x => x.IdTipoIncidencia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Entrega>(e =>
        {
            e.ToTable("entrega", "dbo", tb => tb.HasTrigger("trg_entrega_valida"));
            e.HasKey(x => x.IdEntrega);
            e.Property(x => x.IdEntrega).ValueGeneratedOnAdd();
            e.Property(x => x.NombreReceptor).HasMaxLength(120);
            e.Property(x => x.RutaFirma).HasMaxLength(300);
            e.Property(x => x.RutaFoto).HasMaxLength(300);
            e.Property(x => x.FechaHora).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.IdGuia).IsUnique();
            e.HasOne(x => x.Guia)
                .WithOne(g => g.Entrega)
                .HasForeignKey<Entrega>(x => x.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Notificacion>(e =>
        {
            e.ToTable("notificacion", "dbo");
            e.HasKey(x => x.IdNotificacion);
            e.Property(x => x.IdNotificacion).ValueGeneratedOnAdd();
            e.Property(x => x.Tipo).HasMaxLength(30).IsRequired();
            e.Property(x => x.Mensaje).HasMaxLength(300).IsRequired();
            e.Property(x => x.Leida).HasDefaultValue(false);
            e.Property(x => x.FechaHora).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Guia)
                .WithMany()
                .HasForeignKey(x => x.IdGuia)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.HojaRuta)
                .WithMany()
                .HasForeignKey(x => x.IdHojaRuta)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------- Vistas (sin clave) ----------
        modelBuilder.Entity<TrazabilidadPublicaView>(e =>
        {
            e.HasNoKey().ToView("vw_trazabilidad_publica", "dbo");
            e.Property(x => x.CodigoGuia).HasColumnName("codigo_guia");
            e.Property(x => x.EstadoActual).HasColumnName("estado_actual");
            e.Property(x => x.TipoEvento).HasColumnName("tipo_evento");
            e.Property(x => x.Hito).HasColumnName("hito");
            e.Property(x => x.OrdenHito).HasColumnName("orden_hito");
            e.Property(x => x.FechaHora).HasColumnName("fecha_hora");
            e.Property(x => x.Estacion).HasColumnName("estacion");
        });

        modelBuilder.Entity<ReporteEnvioView>(e =>
        {
            e.HasNoKey().ToView("vw_reporte_envios", "dbo");
            e.Property(x => x.IdGuia).HasColumnName("id_guia");
            e.Property(x => x.CodigoGuia).HasColumnName("codigo_guia");
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro");
            e.Property(x => x.Cliente).HasColumnName("cliente");
            e.Property(x => x.AlmacenOrigen).HasColumnName("almacen_origen");
            e.Property(x => x.TipoCarga).HasColumnName("tipo_carga");
            e.Property(x => x.Destinatario).HasColumnName("destinatario");
            e.Property(x => x.DireccionDestino).HasColumnName("direccion_destino");
            e.Property(x => x.PesoKg).HasColumnName("peso_kg").HasPrecision(10, 2);
            e.Property(x => x.VolumenM3).HasColumnName("volumen_m3").HasPrecision(10, 3);
            e.Property(x => x.EstadoActual).HasColumnName("estado_actual");
            e.Property(x => x.EstadoHojaRuta).HasColumnName("estado_hoja_ruta");
            e.Property(x => x.Vehiculo).HasColumnName("vehiculo");
            e.Property(x => x.Conductor).HasColumnName("conductor");
            e.Property(x => x.FechaEntrega).HasColumnName("fecha_entrega");
            e.Property(x => x.HorasTransito).HasColumnName("horas_transito").HasPrecision(18, 4);
            e.Property(x => x.TotalIncidencias).HasColumnName("total_incidencias");
        });
    }
}
