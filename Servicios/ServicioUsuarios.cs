using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;

namespace Nomenclador.Web.Servicios;

public class ServicioUsuarios(AppDb db, IPasswordHasher<Usuario> hasher)
{
    public const int LargoMinimoClave = 8;

    public async Task<Usuario?> ValidarAsync(string nombreUsuario, string clave)
    {
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.NombreUsuario == nombreUsuario.Trim().ToLower());
        if (u is null || !u.Activo) { hasher.HashPassword(new Usuario(), clave); return null; } // tiempo constante aprox.
        var r = hasher.VerifyHashedPassword(u, u.ClaveHash, clave);
        if (r == PasswordVerificationResult.Failed) return null;
        if (r == PasswordVerificationResult.SuccessRehashNeeded) u.ClaveHash = hasher.HashPassword(u, clave);
        u.UltimoIngresoUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return u;
    }

    public void FijarClave(Usuario u, string clave, bool temporal)
    {
        u.ClaveHash = hasher.HashPassword(u, clave);
        u.DebeCambiarClave = temporal;
        u.SelloSeguridad = Guid.NewGuid().ToString("N");
    }

    public static string? ValidarClaveNueva(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < LargoMinimoClave)
            return $"La clave debe tener al menos {LargoMinimoClave} caracteres.";
        return null;
    }

    public static ClaimsPrincipal CrearPrincipal(Usuario u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.NombreUsuario),
            new("nombre", u.NombreCompleto),
            new(ClaimTypes.Role, u.Rol),
            new("sello", u.SelloSeguridad),
        };
        if (u.DebeCambiarClave) claims.Add(new Claim("debe_cambiar_clave", "1"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static async Task IniciarSesionAsync(HttpContext ctx, Usuario u, bool recordar)
    {
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CrearPrincipal(u),
            new AuthenticationProperties { IsPersistent = recordar });
    }

    /// <summary>Corta la sesión si el usuario fue desactivado o cambió su sello (clave/rol).</summary>
    public static async Task ValidarSesionAsync(CookieValidatePrincipalContext ctx)
    {
        var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var sello = ctx.Principal?.FindFirstValue("sello");
        if (!int.TryParse(id, out var uid)) { ctx.RejectPrincipal(); return; }

        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDb>();
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == uid);
        if (u is null || !u.Activo || u.SelloSeguridad != sello)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

public static class ExtensionesUsuario
{
    public static int Id(this ClaimsPrincipal p) => int.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static string NombreVisible(this ClaimsPrincipal p) => p.FindFirstValue("nombre") ?? p.Identity?.Name ?? "";
    public static bool EsAdmin(this ClaimsPrincipal p) => p.IsInRole(Roles.Admin);
}
