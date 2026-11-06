using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages;

public partial class IndexModel(AppDb db, Calculadora calc) : PageModel
{
    public const int TamPagina = 50;

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public string? Seccion { get; set; }
    [BindProperty(SupportsGet = true)] public string? Grupo { get; set; }
    /// <summary>vigentes (default) | nuevos | eliminados | todos</summary>
    [BindProperty(SupportsGet = true)] public string Estado { get; set; } = "vigentes";
    [BindProperty(SupportsGet = true)] public bool ConValores { get; set; }
    [BindProperty(SupportsGet = true)] public int? Juego { get; set; }
    [BindProperty(SupportsGet = true, Name = "p")] public int Pagina { get; set; } = 1;

    public List<Seccion> Secciones { get; set; } = new();
    public List<Grupo> GruposSeccion { get; set; } = new();
    public List<JuegoValores> Juegos { get; set; } = new();
    public JuegoValores? JuegoActual { get; set; }
    public Dictionary<string, decimal> Valores { get; set; } = new();
    public List<Practica> Resultados { get; set; } = new();
    public int Total { get; set; }
    public int Paginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamPagina));

    [GeneratedRegex(@"^(\d{1,2}|OD|ON)(\.\d{0,2}){0,2}\.?$", RegexOptions.IgnoreCase)]
    private static partial Regex PatronCodigo();

    public async Task OnGetAsync()
    {
        Secciones = await db.Secciones.AsNoTracking().OrderBy(s => s.Codigo).ToListAsync();
        if (!string.IsNullOrEmpty(Seccion))
            GruposSeccion = await db.Grupos.AsNoTracking().Where(g => g.SeccionCodigo == Seccion).OrderBy(g => g.Codigo).ToListAsync();
        Juegos = await db.JuegosValores.AsNoTracking().Where(j => j.Activo).OrderByDescending(j => j.PorDefecto).ThenBy(j => j.Nombre).ToListAsync();
        (JuegoActual, Valores) = await calc.JuegoAsync(Juego);

        var query = db.Practicas.AsNoTracking().Include(p => p.Grupo!).ThenInclude(g => g.Seccion).AsQueryable();

        var q = Q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            if (PatronCodigo().IsMatch(q))
            {
                // Búsqueda por código: "01.02" o "0102" o "01.02.1"
                var cod = q.TrimEnd('.').ToUpperInvariant();
                query = query.Where(p => p.Codigo.StartsWith(cod));
            }
            else
            {
                foreach (var palabra in AppDb.Normalizar(q).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var w = palabra;
                    query = query.Where(p => p.TextoBusqueda.Contains(w));
                }
            }
        }
        if (!string.IsNullOrEmpty(Grupo)) query = query.Where(p => p.GrupoCodigo == Grupo);
        else if (!string.IsNullOrEmpty(Seccion)) query = query.Where(p => p.Grupo!.SeccionCodigo == Seccion);

        query = Estado switch
        {
            "nuevos" => query.Where(p => p.Estado == "N"),
            "eliminados" => query.Where(p => p.Estado == "E"),
            "todos" => query,
            _ => query.Where(p => p.Estado != "E"),
        };
        if (ConValores)
            query = query.Where(p => p.UEspecialista != null || p.UGasto != null || p.UAnestesista != null || p.UAyudante != null);

        Total = await query.CountAsync();
        Pagina = Math.Clamp(Pagina, 1, Paginas);
        Resultados = await query.OrderBy(p => p.Codigo).Skip((Pagina - 1) * TamPagina).Take(TamPagina).ToListAsync();
    }

    public Dictionary<string, string?> RutaCon(int pagina) => new()
    {
        ["q"] = Q, ["seccion"] = Seccion, ["grupo"] = Grupo, ["estado"] = Estado,
        ["conValores"] = ConValores ? "true" : null, ["juego"] = Juego?.ToString(), ["p"] = pagina.ToString(),
    };
}
