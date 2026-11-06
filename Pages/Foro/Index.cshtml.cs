using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;

namespace Nomenclador.Web.Pages.Foro;

public class IndexModel(AppDb db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Categoria { get; set; }
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public string? Codigo { get; set; }

    public record Fila(int Id, string Titulo, string Categoria, string? CodigoPractica, bool Fijado, bool Cerrado,
        string Autor, DateTime CreadoUtc, DateTime UltimaActividadUtc, int Respuestas);

    public List<Fila> Temas { get; set; } = new();

    public async Task OnGetAsync()
    {
        var q = db.Temas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(Categoria)) q = q.Where(t => t.Categoria == Categoria);
        if (!string.IsNullOrEmpty(Codigo)) q = q.Where(t => t.CodigoPractica == Codigo);
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var w = Q.Trim();
            q = q.Where(t => EF.Functions.Like(t.Titulo, $"%{w}%") || EF.Functions.Like(t.Cuerpo, $"%{w}%"));
        }
        Temas = await q.OrderByDescending(t => t.Fijado).ThenByDescending(t => t.UltimaActividadUtc).Take(200)
            .Select(t => new Fila(t.Id, t.Titulo, t.Categoria, t.CodigoPractica, t.Fijado, t.Cerrado,
                t.Autor!.NombreCompleto, t.CreadoUtc, t.UltimaActividadUtc, t.Respuestas.Count))
            .ToListAsync();
    }
}
