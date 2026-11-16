using Microsoft.Data.SqlClient;
using POS.Shared.DTOs;
using System.Data;

namespace POS.Server.Infrastructure;

/// <summary>
/// Traduce los errores de SQL Server, especialmente los lanzados por los
/// triggers de reglas de negocio (50001-50025) y las restricciones (547, 2627).
/// </summary>
public static class SqlErrorTranslator
{
    private static readonly Dictionary<string, string> Restricciones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CK_entrega_comprobante"] = "RN07: la entrega requiere firma del receptor o fotografía como comprobante.",
        ["CK_usuario_nombre"] = "El nombre del usuario no puede estar vacío.",
        ["CK_usuario_correo"] = "El correo del usuario no tiene un formato válido.",
        ["CK_cliente_razon"] = "La razón social del cliente no puede estar vacía.",
        ["CK_cliente_nit"] = "El NIT del cliente no puede estar vacío.",
        ["CK_almacen_tipo"] = "El tipo de almacén debe ser 'Almacén' o 'Centro de Distribución'.",
        ["CK_vehiculo_kg"] = "La capacidad en kg del vehículo debe ser mayor a cero.",
        ["CK_vehiculo_m3"] = "La capacidad en m3 del vehículo debe ser mayor a cero.",
        ["CK_vehiculo_estado"] = "El estado del vehículo no es válido.",
        ["CK_hoja_estado"] = "El estado de la hoja de ruta no es válido.",
        ["CK_guia_peso"] = "El peso de la guía debe ser mayor a cero.",
        ["CK_guia_vol"] = "El volumen de la guía debe ser mayor a cero.",
        ["CK_guia_destinatario"] = "El destinatario de la guía no puede estar vacío.",
        ["CK_guia_direccion"] = "La dirección de destino no puede estar vacía.",
        ["CK_detalle_cantidad"] = "La cantidad despachada debe ser mayor a cero.",
        ["CK_mov_tipo"] = "El tipo de movimiento debe ser ENTRADA o SALIDA.",
        ["CK_mov_cantidad"] = "La cantidad del movimiento debe ser mayor a cero.",
        ["CK_incidencia_desc"] = "La descripción de la incidencia no puede estar vacía.",
        ["CK_incidencia_foto"] = "La incidencia requiere una fotografía de evidencia (RN08).",
        ["CK_inventario_stock"] = "El stock del inventario no puede ser negativo.",
        ["CK_notif_tipo"] = "El tipo de notificación no es válido.",
        ["CK_notif_mensaje"] = "El mensaje de la notificación no puede estar vacío."
    };

    public static ApiError Translate(Exception ex)
    {
        if (ex is BusinessException business)
        {
            return new ApiError
            {
                Codigo = business.Codigo,
                Mensaje = business.Message
            };
        }

        if (ex is KeyNotFoundException)
        {
            return new ApiError { Codigo = 404, Mensaje = ex.Message };
        }

        if (ex is UnauthorizedAccessException)
        {
            return new ApiError { Codigo = 403, Mensaje = ex.Message };
        }

        var sql = FindSqlException(ex);
        if (sql == null)
        {
            return new ApiError
            {
                Codigo = 500,
                Mensaje = "Ocurrió un error inesperado en el servidor.",
                Detalle = ex.Message
            };
        }

        return sql.Number switch
        {
            >= 50001 and <= 50025 => new ApiError
            {
                Codigo = sql.Number,
                Mensaje = Limpiar(sql.Message)
            },
            547 => TranslateRestriction(sql),
            2627 or 2601 => new ApiError
            {
                Codigo = sql.Number,
                Mensaje = "Ya existe un registro con los mismos datos únicos. Verifique el correo, NIT, placa, SKU o código.",
                Detalle = sql.Message
            },
            3609 => new ApiError
            {
                Codigo = 400,
                Mensaje = "La operación fue rechazada por una regla de negocio de la base de datos.",
                Detalle = sql.Message
            },
            _ => new ApiError
            {
                Codigo = sql.Number,
                Mensaje = "Error de base de datos: " + Limpiar(sql.Message),
                Detalle = sql.Message
            }
        };
    }

    public static int StatusCodeFor(ApiError error) => error.Codigo switch
    {
        403 => StatusCodes.Status403Forbidden,
        404 => StatusCodes.Status404NotFound,
        409 or 2627 or 2601 => StatusCodes.Status409Conflict,
        >= 50001 and <= 50025 => StatusCodes.Status400BadRequest,
        547 => StatusCodes.Status400BadRequest,
        _ => error.Codigo is >= 400 and < 500 ? error.Codigo : StatusCodes.Status500InternalServerError
    };

    private static ApiError TranslateRestriction(SqlException sql)
    {
        foreach (var (restriccion, mensaje) in Restricciones)
        {
            if (sql.Message.Contains(restriccion, StringComparison.OrdinalIgnoreCase))
            {
                return new ApiError { Codigo = 547, Mensaje = mensaje };
            }
        }

        if (sql.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiError
            {
                Codigo = 547,
                Mensaje = "El registro hace referencia a un dato que no existe o está siendo utilizado.",
                Detalle = Limpiar(sql.Message)
            };
        }

        return new ApiError
        {
            Codigo = 547,
            Mensaje = "Los datos no cumplen una validación de la base de datos.",
            Detalle = Limpiar(sql.Message)
        };
    }

    private static SqlException? FindSqlException(Exception? ex)
    {
        while (ex != null)
        {
            if (ex is SqlException sql)
            {
                return sql;
            }
            ex = ex.InnerException;
        }
        return null;
    }

    private static string Limpiar(string message)
    {
        var index = message.IndexOf("The statement has been terminated", StringComparison.OrdinalIgnoreCase);
        if (index > 0)
        {
            message = message[..index].Trim();
        }

        index = message.IndexOf("\n", StringComparison.Ordinal);
        return index > 0 ? message[..index].Trim() : message.Trim();
    }
}
