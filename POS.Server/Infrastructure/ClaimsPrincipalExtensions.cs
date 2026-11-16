using System.Security.Claims;

namespace POS.Server.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub")
                    ?? user.FindFirstValue("nameid");

        if (!int.TryParse(value, out var id))
        {
            throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");
        }

        return id;
    }

    public static int GetRolId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("id_rol");
        return int.TryParse(value, out var id) ? id : 0;
    }

    public static string GetRoleName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role)
               ?? user.FindFirstValue("role")
               ?? string.Empty;
    }
}
