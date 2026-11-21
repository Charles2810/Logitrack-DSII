using POS.Shared.Constants;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Infrastructure;

public static class GuiaQueries
{
    public static IQueryable<GuiaDto> ProyectarResumen(this IQueryable<GuiaEnvio> query) =>
        query.Select(g => new GuiaDto
        {
            IdGuia = g.IdGuia,
            CodigoGuia = g.CodigoGuia,
            IdCliente = g.IdCliente,
            Cliente = g.Cliente!.RazonSocial,
            IdAlmacenOrigen = g.IdAlmacenOrigen,
            AlmacenOrigen = g.AlmacenOrigen!.Nombre,
            IdTipoCarga = g.IdTipoCarga,
            TipoCarga = g.TipoCarga!.Nombre,
            IdHitoActual = g.IdHitoActual,
            HitoActual = g.HitoActual!.Nombre,
            OrdenHito = g.HitoActual!.Orden,
            IdHojaRuta = g.IdHojaRuta,
            EstadoHojaRuta = g.HojaRuta != null ? g.HojaRuta.Estado : null,
            IdUsuarioRegistro = g.IdUsuarioRegistro,
            UsuarioRegistro = g.UsuarioRegistro!.NombreCompleto,
            Destinatario = g.Destinatario,
            DireccionDestino = g.DireccionDestino,
            LatitudOrigen = (double?)g.AlmacenOrigen!.Latitud,
            LongitudOrigen = (double?)g.AlmacenOrigen!.Longitud,
            LatitudDestino = (double?)g.LatitudDestino,
            LongitudDestino = (double?)g.LongitudDestino,
            DistanciaEstimadaKm = g.DistanciaEstimadaKm,
            DuracionEstimadaMin = g.DuracionEstimadaMin,
            PesoKg = g.PesoKg,
            VolumenM3 = g.VolumenM3,
            FechaRegistro = g.FechaRegistro,
            TotalDetalles = g.Detalles.Count,
            TotalIncidencias = g.Incidencias.Count,
            TieneEntrega = g.Entrega != null
        });

    public static IQueryable<GuiaDto> ProyectarDetalle(this IQueryable<GuiaEnvio> query) =>
        query.Select(g => new GuiaDto
        {
            IdGuia = g.IdGuia,
            CodigoGuia = g.CodigoGuia,
            IdCliente = g.IdCliente,
            Cliente = g.Cliente!.RazonSocial,
            IdAlmacenOrigen = g.IdAlmacenOrigen,
            AlmacenOrigen = g.AlmacenOrigen!.Nombre,
            IdTipoCarga = g.IdTipoCarga,
            TipoCarga = g.TipoCarga!.Nombre,
            IdHitoActual = g.IdHitoActual,
            HitoActual = g.HitoActual!.Nombre,
            OrdenHito = g.HitoActual!.Orden,
            IdHojaRuta = g.IdHojaRuta,
            EstadoHojaRuta = g.HojaRuta != null ? g.HojaRuta.Estado : null,
            IdUsuarioRegistro = g.IdUsuarioRegistro,
            UsuarioRegistro = g.UsuarioRegistro!.NombreCompleto,
            Destinatario = g.Destinatario,
            DireccionDestino = g.DireccionDestino,
            LatitudOrigen = (double?)g.AlmacenOrigen!.Latitud,
            LongitudOrigen = (double?)g.AlmacenOrigen!.Longitud,
            LatitudDestino = (double?)g.LatitudDestino,
            LongitudDestino = (double?)g.LongitudDestino,
            DistanciaEstimadaKm = g.DistanciaEstimadaKm,
            DuracionEstimadaMin = g.DuracionEstimadaMin,
            PesoKg = g.PesoKg,
            VolumenM3 = g.VolumenM3,
            FechaRegistro = g.FechaRegistro,
            TotalDetalles = g.Detalles.Count,
            TotalIncidencias = g.Incidencias.Count,
            TieneEntrega = g.Entrega != null,
            Detalles = g.Detalles
                .OrderBy(d => d.Producto!.Nombre)
                .Select(d => new DetalleGuiaDto
                {
                    IdProducto = d.IdProducto,
                    Sku = d.Producto!.Sku,
                    Producto = d.Producto!.Nombre,
                    Cantidad = d.Cantidad
                })
                .ToList()
        });

    public static bool EsEntregada(this GuiaEnvio guia, string hitoNombre) =>
        hitoNombre == Hitos.Entregado;
}
