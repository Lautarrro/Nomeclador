using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;

namespace Nomenclador.Web.Pages.Admin;

public partial class PracticaEditarModel(AppDb db) : PageModel
{
    public class Formulario
    {
        public int Id { get; set; }
        public int Version { get; set; }
        [Required(ErrorMessage = "Falta el código")]
        public string Codigo { get; set; } = "";
        public string? Estado { get; set; } = "";
        [Required(ErrorMessage = "Falta la descripción"), StringLength(2000)]
        public string Descripcion { get; set; } = "";
        [Range(0, 100000)] public decimal? UEspecialista { get; set; }
        [Range(0, 10)] public int? AyudantesCantidad { get; set; }
        [Range(0, 100000)] public decimal? UAyudante { get; set; }
        [Range(0, 100000)] public decimal? UAnestesista { get; set; }
        [Range(0, 100000)] public decimal? UGasto { get; set; }
        public string? CoefGastoOverride { get; set; }
        [StringLength(4000)] public string? Nota { get; set; }
    }

    [BindProperty] public Formulario F { get; set; } = new();
    public List<Grupo> Grupos { get; set; } = new();
    public bool EsNueva => F.Id == 0;

    [GeneratedRegex(@"^(\d{2}|OD|ON)(\.\d{2}){1,2}\.\d{2,3}[a-z]?$")]
    private static partial Regex PatronCodigo();

    private async Task CargarListasAsync() =>
        Grupos = await db.Grupos.AsNoTracking().OrderBy(g => g.Codigo).ToListAsync();

    public async Task<IActionResult> OnGetAsync(int? id, string? grupo)
    {
        await CargarListasAsync();
        if (id is null)
        {
            if (!string.IsNullOrEmpty(grupo)) F.Codigo = grupo + ".";
            return Page();
        }
        var p = await db.Practicas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        F = new Formulario
        {
            Id = p.Id, Version = p.Version, Codigo = p.Codigo, Estado = p.Estado, Descripcion = p.Descripcion,
            UEspecialista = p.UEspecialista, AyudantesCantidad = p.AyudantesCantidad, UAyudante = p.UAyudante,
            UAnestesista = p.UAnestesista, UGasto = p.UGasto, CoefGastoOverride = p.CoefGastoOverride, Nota = p.Nota,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        F.Codigo = F.Codigo.Trim();
        if (!PatronCodigo().IsMatch(F.Codigo))
            ModelState.AddModelError("F.Codigo", "El código debe tener el formato 00.00.00");
        var grupoCod = string.Join('.', F.Codigo.Split('.').SkipLast(1));
        if (ModelState.IsValid && !await db.Grupos.AnyAsync(g => g.Codigo == grupoCod))
            ModelState.AddModelError("F.Codigo", $"No existe el grupo {grupoCod}. Crealo primero en Capítulos y grupos.");
        if (await db.Practicas.AnyAsync(p => p.Codigo == F.Codigo && p.Id != F.Id))
            ModelState.AddModelError("F.Codigo", "Ya existe otra práctica con ese código.");
        if (F.Estado is not ("N" or "E")) F.Estado = "";
        if (string.IsNullOrEmpty(F.CoefGastoOverride) || Coeficientes.Buscar(F.CoefGastoOverride) is null) F.CoefGastoOverride = null;
        if (F.UAyudante is > 0 && F.AyudantesCantidad is null) F.AyudantesCantidad = 1;

        if (!ModelState.IsValid) { await CargarListasAsync(); return Page(); }

        Practica p;
        if (EsNueva)
        {
            p = new Practica();
            db.Practicas.Add(p);
        }
        else
        {
            p = await db.Practicas.FirstOrDefaultAsync(x => x.Id == F.Id) ?? throw new InvalidOperationException();
            // Control de concurrencia: si otro admin la guardó mientras tanto, avisamos.
            db.Entry(p).Property(x => x.Version).OriginalValue = F.Version;
        }
        p.Codigo = F.Codigo;
        p.GrupoCodigo = grupoCod;
        p.Estado = F.Estado ?? "";
        p.Descripcion = F.Descripcion.Trim();
        p.UEspecialista = F.UEspecialista;
        p.AyudantesCantidad = F.UAyudante is > 0 ? F.AyudantesCantidad : null;
        p.UAyudante = F.UAyudante;
        p.UAnestesista = F.UAnestesista;
        p.UGasto = F.UGasto;
        p.CoefGastoOverride = F.CoefGastoOverride;
        p.Nota = string.IsNullOrWhiteSpace(F.Nota) ? null : F.Nota.Trim();
        p.EditadaPorAdmin = true;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Otra persona modificó esta práctica mientras la editabas. Se recargaron los datos actuales; revisalos y volvé a guardar.";
            return RedirectToPage(new { id = F.Id });
        }
        TempData["Mensaje"] = $"Práctica {p.Codigo} guardada.";
        return RedirectToPage("/Index", new { q = p.Codigo, estado = "todos" });
    }

    public async Task<IActionResult> OnPostEliminarAsync()
    {
        var p = await db.Practicas.FindAsync(F.Id);
        if (p is null) return NotFound();
        // No se borra: se marca como eliminada para conservar el historial y no romper temas del foro.
        p.Estado = "E";
        p.EditadaPorAdmin = true;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Práctica {p.Codigo} marcada como eliminada.";
        return RedirectToPage("/Index", new { q = p.Codigo, estado = "todos" });
    }
}
