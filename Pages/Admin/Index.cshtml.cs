using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Data.Seed;
using Nomenclador.Web.Herramientas;

namespace Nomenclador.Web.Pages.Admin;

public class IndexModel(AppDb db) : PageModel
{
    public int Practicas { get; set; }
    public int ConValores { get; set; }
    public int EditadasAdmin { get; set; }
    public int Secciones { get; set; }
    public int Usuarios { get; set; }
    public int EnSeed => NomencladorSeed.Practicas.Length;

    public async Task OnGetAsync()
    {
        Practicas = await db.Practicas.CountAsync();
        ConValores = await db.Practicas.CountAsync(p => p.UEspecialista != null || p.UGasto != null);
        EditadasAdmin = await db.Practicas.CountAsync(p => p.EditadaPorAdmin);
        Secciones = await db.Secciones.CountAsync();
        Usuarios = await db.Usuarios.CountAsync(u => u.Activo);
    }

    /// <summary>Descarga NomencladorSeed.cs generado con el contenido actual de la base.</summary>
    public async Task<IActionResult> OnGetExportarAsync()
    {
        var secciones = await db.Secciones.AsNoTracking().OrderBy(s => s.Codigo).ToListAsync();
        var grupos = await db.Grupos.AsNoTracking().OrderBy(g => g.Codigo).ToListAsync();
        var practicas = await db.Practicas.AsNoTracking().OrderBy(p => p.Codigo).ToListAsync();
        var cs = SeedWriter.Generar(
            secciones.Select(s => new SeccionSeed(s.Codigo, s.Nombre)),
            grupos.Select(g => new GrupoSeed(g.Codigo, g.SeccionCodigo, g.Nombre, g.Nota)),
            practicas.Select(p => new PracticaSeed(p.Codigo, p.Estado, p.Descripcion, p.UEspecialista, p.AyudantesCantidad,
                p.UAyudante, p.UAnestesista, p.UGasto, p.CoefGastoOverride, p.Nota)),
            $"la base de datos (exportado por {User.Identity?.Name})");
        return File(new UTF8Encoding(true).GetBytes(cs), "text/plain", "NomencladorSeed.cs");
    }

    /// <summary>Vuelve a aplicar NomencladorSeed.cs (agrega lo nuevo, no pisa lo editado por admin).</summary>
    public async Task<IActionResult> OnPostRecargarAsync()
    {
        var (nuevas, act) = await Inicializador.CargarNomencladorAsync(db);
        TempData["Mensaje"] = $"Carga inicial aplicada: {nuevas} prácticas nuevas, {act} actualizadas.";
        return RedirectToPage();
    }
}
