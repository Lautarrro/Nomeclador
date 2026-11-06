using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;

namespace Nomenclador.Web.Pages.Admin;

public class JuegosModel(AppDb db) : PageModel
{
    public List<JuegoValores> Juegos { get; set; } = new();

    public async Task OnGetAsync()
    {
        Juegos = await db.JuegosValores.AsNoTracking().Include(j => j.Valores)
            .OrderByDescending(j => j.PorDefecto).ThenBy(j => j.Nombre).ToListAsync();
    }

    public async Task<IActionResult> OnPostGuardarAsync(int id, string nombre, string? descripcion, bool activo, Dictionary<string, string> valores)
    {
        var j = await db.JuegosValores.Include(x => x.Valores).FirstOrDefaultAsync(x => x.Id == id);
        if (j is null) return NotFound();
        if (string.IsNullOrWhiteSpace(nombre)) { TempData["Error"] = "El juego necesita un nombre."; return RedirectToPage(); }

        foreach (var d in Coeficientes.Todos)
        {
            if (!valores.TryGetValue(d.Clave, out var txt) || string.IsNullOrWhiteSpace(txt)) continue;
            if (!decimal.TryParse(txt.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) || v < 0)
            {
                TempData["Error"] = $"Valor inválido para {d.Nombre}: '{txt}'.";
                return RedirectToPage();
            }
            var fila = j.Valores.FirstOrDefault(x => x.Clave == d.Clave);
            if (fila is null) j.Valores.Add(new ValorCoeficiente { Clave = d.Clave, Valor = v });
            else fila.Valor = v;
        }
        j.Nombre = nombre.Trim();
        j.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        j.Activo = activo || j.PorDefecto;
        j.ActualizadoUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Juego de valores “{j.Nombre}” guardado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDuplicarAsync(int id)
    {
        var j = await db.JuegosValores.AsNoTracking().Include(x => x.Valores).FirstOrDefaultAsync(x => x.Id == id);
        if (j is null) return NotFound();
        var copia = new JuegoValores
        {
            Nombre = $"{j.Nombre} (copia)",
            Descripcion = j.Descripcion,
            Valores = j.Valores.Select(v => new ValorCoeficiente { Clave = v.Clave, Valor = v.Valor }).ToList(),
        };
        db.JuegosValores.Add(copia);
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Juego duplicado. Cambiale el nombre y los valores.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPorDefectoAsync(int id)
    {
        var todos = await db.JuegosValores.ToListAsync();
        if (!todos.Any(j => j.Id == id)) return NotFound();
        foreach (var j in todos) j.PorDefecto = j.Id == id;
        todos.First(j => j.Id == id).Activo = true;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
