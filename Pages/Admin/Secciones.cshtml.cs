using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;

namespace Nomenclador.Web.Pages.Admin;

public partial class SeccionesModel(AppDb db) : PageModel
{
    public List<Seccion> Secciones { get; set; } = new();
    public Dictionary<string, int> PracticasPorGrupo { get; set; } = new();

    [GeneratedRegex(@"^\d{2}$")] private static partial Regex PatronSeccion();
    [GeneratedRegex(@"^\d{2}\.\d{2}$")] private static partial Regex PatronGrupo();

    public async Task OnGetAsync()
    {
        Secciones = await db.Secciones.AsNoTracking().Include(s => s.Grupos.OrderBy(g => g.Codigo)).OrderBy(s => s.Codigo).ToListAsync();
        PracticasPorGrupo = await db.Practicas.GroupBy(p => p.GrupoCodigo).Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N);
    }

    public async Task<IActionResult> OnPostGuardarSeccionAsync(string codigo, string nombre, string coefHonorario, string coefGasto)
    {
        var s = await db.Secciones.FindAsync(codigo);
        if (s is null) return NotFound();
        if (Coeficientes.Buscar(coefHonorario) is null || Coeficientes.Buscar(coefGasto) is null) return BadRequest();
        s.Nombre = (nombre ?? "").Trim();
        s.CoefHonorario = coefHonorario;
        s.CoefGasto = coefGasto;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Capítulo {codigo} guardado.";
        return RedirectToPage(null, null, "s" + codigo);
    }

    public async Task<IActionResult> OnPostNuevaSeccionAsync(string codigo, string nombre)
    {
        codigo = (codigo ?? "").Trim();
        if (!PatronSeccion().IsMatch(codigo)) { TempData["Error"] = "El código de capítulo son 2 dígitos (ej. 14)."; return RedirectToPage(); }
        if (await db.Secciones.AnyAsync(s => s.Codigo == codigo)) { TempData["Error"] = $"El capítulo {codigo} ya existe."; return RedirectToPage(); }
        var (h, g) = Coeficientes.PorDefectoParaSeccion(codigo);
        db.Secciones.Add(new Seccion { Codigo = codigo, Nombre = (nombre ?? "").Trim(), CoefHonorario = h, CoefGasto = g });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Capítulo {codigo} creado.";
        return RedirectToPage(null, null, "s" + codigo);
    }

    public async Task<IActionResult> OnPostGuardarGrupoAsync(string codigo, string nombre, string? nota)
    {
        var g = await db.Grupos.FindAsync(codigo);
        if (g is null) return NotFound();
        g.Nombre = (nombre ?? "").Trim();
        g.Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Grupo {codigo} guardado.";
        return RedirectToPage(null, null, "s" + g.SeccionCodigo);
    }

    public async Task<IActionResult> OnPostNuevoGrupoAsync(string seccion, string codigo, string nombre)
    {
        codigo = (codigo ?? "").Trim();
        if (!PatronGrupo().IsMatch(codigo) || !codigo.StartsWith(seccion + "."))
        {
            TempData["Error"] = $"El código de grupo debe ser {seccion}.NN";
            return RedirectToPage(null, null, "s" + seccion);
        }
        if (await db.Grupos.AnyAsync(g => g.Codigo == codigo)) { TempData["Error"] = $"El grupo {codigo} ya existe."; return RedirectToPage(null, null, "s" + seccion); }
        db.Grupos.Add(new Grupo { Codigo = codigo, SeccionCodigo = seccion, Nombre = (nombre ?? "").Trim() });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Grupo {codigo} creado.";
        return RedirectToPage(null, null, "s" + seccion);
    }
}
