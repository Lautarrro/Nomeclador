using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages.Foro;

public class TemaModel(AppDb db) : PageModel
{
    public TemaForo Tema { get; set; } = null!;
    public Practica? Practica { get; set; }

    [BindProperty, Required(ErrorMessage = "Escribí una respuesta"), StringLength(20000)]
    public string Respuesta { get; set; } = "";

    private async Task<bool> CargarAsync(int id)
    {
        var t = await db.Temas.AsNoTracking().Include(t => t.Autor)
            .Include(t => t.Respuestas.OrderBy(r => r.CreadoUtc)).ThenInclude(r => r.Autor)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (t is null) return false;
        Tema = t;
        if (t.CodigoPractica is not null)
            Practica = await db.Practicas.AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == t.CodigoPractica);
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id) => await CargarAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostResponderAsync(int id)
    {
        var tema = await db.Temas.FindAsync(id);
        if (tema is null) return NotFound();
        if (tema.Cerrado && !User.EsAdmin()) ModelState.AddModelError("", "El tema está cerrado.");
        if (!ModelState.IsValid) { await CargarAsync(id); return Page(); }

        db.Respuestas.Add(new RespuestaForo { TemaForoId = id, Cuerpo = Respuesta.Trim(), AutorId = User.Id() });
        tema.UltimaActividadUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Redirect(Url.Page("/Foro/Tema", new { id }) + "#fin");
    }

    public async Task<IActionResult> OnPostBorrarRespuestaAsync(int id, int respuestaId)
    {
        var r = await db.Respuestas.FirstOrDefaultAsync(x => x.Id == respuestaId && x.TemaForoId == id);
        if (r is null) return NotFound();
        if (r.AutorId != User.Id() && !User.EsAdmin()) return Forbid();
        db.Respuestas.Remove(r);
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostBorrarTemaAsync(int id)
    {
        var t = await db.Temas.Include(x => x.Respuestas).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!User.EsAdmin() && (t.AutorId != User.Id() || t.Respuestas.Count > 0)) return Forbid();
        db.Respuestas.RemoveRange(t.Respuestas);
        db.Temas.Remove(t);
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Tema eliminado.";
        return RedirectToPage("/Foro/Index");
    }

    public async Task<IActionResult> OnPostModerarAsync(int id, string accion)
    {
        if (!User.EsAdmin()) return Forbid();
        var t = await db.Temas.FindAsync(id);
        if (t is null) return NotFound();
        switch (accion)
        {
            case "fijar": t.Fijado = !t.Fijado; break;
            case "cerrar": t.Cerrado = !t.Cerrado; break;
        }
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }
}
