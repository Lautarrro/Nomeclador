using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages.Foro;

public class NuevoModel(AppDb db) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Poné un título"), StringLength(200)]
    public string Titulo { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Escribí el mensaje"), StringLength(20000)]
    public string Cuerpo { get; set; } = "";

    [BindProperty] public string Categoria { get; set; } = "Consultas";

    [BindProperty(SupportsGet = true), StringLength(12)]
    public string? Codigo { get; set; }

    [BindProperty] public bool Fijado { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!CategoriasForo.Todas.Contains(Categoria)) Categoria = "General";
        if (Categoria == "Novedades" && !User.EsAdmin())
            ModelState.AddModelError(nameof(Categoria), "Solo los administradores publican novedades.");
        Codigo = string.IsNullOrWhiteSpace(Codigo) ? null : Codigo.Trim();
        if (Codigo is not null && !await db.Practicas.AnyAsync(p => p.Codigo == Codigo))
            ModelState.AddModelError(nameof(Codigo), "Ese código no existe en el nomenclador.");
        if (!ModelState.IsValid) return Page();

        var t = new TemaForo
        {
            Titulo = Titulo.Trim(),
            Cuerpo = Cuerpo.Trim(),
            Categoria = Categoria,
            CodigoPractica = Codigo,
            Fijado = Fijado && User.EsAdmin(),
            AutorId = User.Id(),
        };
        db.Temas.Add(t);
        await db.SaveChangesAsync();
        return RedirectToPage("/Foro/Tema", new { id = t.Id });
    }
}
