using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nomenclador.Web.Data;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages.Cuenta;

public class CambiarClaveModel(AppDb db, ServicioUsuarios usuarios, IPasswordHasher<Usuario> hasher) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Ingresá tu clave actual"), DataType(DataType.Password)]
    public string Actual { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Ingresá la clave nueva"), DataType(DataType.Password)]
    public string Nueva { get; set; } = "";

    [BindProperty, DataType(DataType.Password)]
    public string Repetir { get; set; } = "";

    public bool Obligatorio => User.HasClaim("debe_cambiar_clave", "1");

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var u = await db.Usuarios.FindAsync(User.Id());
        if (u is null) return RedirectToPage("/Cuenta/Ingresar");

        if (hasher.VerifyHashedPassword(u, u.ClaveHash, Actual) == PasswordVerificationResult.Failed)
            ModelState.AddModelError(nameof(Actual), "La clave actual no es correcta.");
        if (ServicioUsuarios.ValidarClaveNueva(Nueva) is string err)
            ModelState.AddModelError(nameof(Nueva), err);
        if (Nueva != Repetir)
            ModelState.AddModelError(nameof(Repetir), "Las claves no coinciden.");
        if (Nueva == Actual)
            ModelState.AddModelError(nameof(Nueva), "La clave nueva tiene que ser distinta de la actual.");
        if (!ModelState.IsValid) return Page();

        usuarios.FijarClave(u, Nueva, temporal: false);
        await db.SaveChangesAsync();
        // Nueva cookie con el sello actualizado (las otras sesiones del usuario se cierran).
        await ServicioUsuarios.IniciarSesionAsync(HttpContext, u, true);
        TempData["Mensaje"] = "Clave actualizada.";
        return RedirectToPage("/Index");
    }
}
