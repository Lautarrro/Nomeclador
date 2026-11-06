using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages.Admin;

public partial class UsuariosModel(AppDb db, ServicioUsuarios servicio) : PageModel
{
    public List<Usuario> Usuarios { get; set; } = new();

    [GeneratedRegex(@"^[a-z0-9._-]{3,60}$")] private static partial Regex PatronUsuario();

    public async Task OnGetAsync() =>
        Usuarios = await db.Usuarios.AsNoTracking().OrderByDescending(u => u.Activo).ThenBy(u => u.NombreUsuario).ToListAsync();

    private static string ClaveTemporal() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)).Replace('/', 'x').Replace('+', 'y');

    public async Task<IActionResult> OnPostCrearAsync(string nombreUsuario, string nombreCompleto, string rol)
    {
        nombreUsuario = (nombreUsuario ?? "").Trim().ToLowerInvariant();
        if (!PatronUsuario().IsMatch(nombreUsuario))
        { TempData["Error"] = "Usuario inválido: 3 a 60 caracteres, letras minúsculas, números, punto, guion."; return RedirectToPage(); }
        if (string.IsNullOrWhiteSpace(nombreCompleto)) { TempData["Error"] = "Falta el nombre completo."; return RedirectToPage(); }
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario)) { TempData["Error"] = "Ese usuario ya existe."; return RedirectToPage(); }

        var u = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto.Trim(),
            Rol = rol == Roles.Admin ? Roles.Admin : Roles.Usuario,
        };
        var clave = ClaveTemporal();
        servicio.FijarClave(u, clave, temporal: true);
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Usuario “{u.NombreUsuario}” creado. Clave temporal: {clave}  (se le pedirá cambiarla al ingresar)";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetearAsync(int id)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        var clave = ClaveTemporal();
        servicio.FijarClave(u, clave, temporal: true);
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Nueva clave temporal para “{u.NombreUsuario}”: {clave}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCambiarAsync(int id, string rol, bool activo)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        rol = rol == Roles.Admin ? Roles.Admin : Roles.Usuario;
        var quedanAdmins = await db.Usuarios.CountAsync(x => x.Id != id && x.Activo && x.Rol == Roles.Admin);
        if ((rol != Roles.Admin || !activo) && u.Rol == Roles.Admin && quedanAdmins == 0)
        {
            TempData["Error"] = "Tiene que quedar al menos un administrador activo.";
            return RedirectToPage();
        }
        if (u.Rol != rol || u.Activo != activo)
        {
            u.Rol = rol;
            u.Activo = activo;
            u.SelloSeguridad = Guid.NewGuid().ToString("N"); // cierra sus sesiones abiertas
            await db.SaveChangesAsync();
        }
        TempData["Mensaje"] = $"Usuario “{u.NombreUsuario}” actualizado.";
        return RedirectToPage();
    }
}
