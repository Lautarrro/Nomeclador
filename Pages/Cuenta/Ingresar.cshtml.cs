using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages.Cuenta;

[EnableRateLimiting("login")]
public class IngresarModel(ServicioUsuarios usuarios) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Ingresá el usuario")]
    public string Usuario { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Ingresá la clave"), DataType(DataType.Password)]
    public string Clave { get; set; } = "";

    [BindProperty]
    public bool Recordar { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var u = await usuarios.ValidarAsync(Usuario, Clave);
        if (u is null)
        {
            Error = "Usuario o clave incorrectos.";
            return Page();
        }
        await ServicioUsuarios.IniciarSesionAsync(HttpContext, u, Recordar);
        if (u.DebeCambiarClave) return RedirectToPage("/Cuenta/CambiarClave");
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }
}
