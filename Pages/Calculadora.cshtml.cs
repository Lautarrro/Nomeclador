using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Servicios;

namespace Nomenclador.Web.Pages;

public class CalculadoraModel(AppDb db, Calculadora calc) : PageModel
{
    public List<JuegoValores> Juegos { get; set; } = new();

    public async Task OnGetAsync()
    {
        Juegos = await db.JuegosValores.AsNoTracking().Where(j => j.Activo)
            .OrderByDescending(j => j.PorDefecto).ThenBy(j => j.Nombre).ToListAsync();
    }

    public record Pedido(int? JuegoId, List<LineaPedido> Lineas);

    public async Task<IActionResult> OnPostCalcularAsync([FromBody] Pedido pedido)
    {
        if (pedido.Lineas is null || pedido.Lineas.Count > 200) return BadRequest();
        var r = await calc.CalcularAsync(pedido.JuegoId, pedido.Lineas);
        return r is null ? BadRequest("No hay juego de valores activo.") : new JsonResult(r);
    }

    /// <summary>Autocompletar: busca por código o texto.</summary>
    public async Task<IActionResult> OnGetBuscarAsync(string q)
    {
        q = (q ?? "").Trim();
        if (q.Length < 2) return new JsonResult(Array.Empty<object>());
        var query = db.Practicas.AsNoTracking();
        if (char.IsDigit(q[0])) query = query.Where(p => p.Codigo.StartsWith(q));
        else
            foreach (var w in AppDb.Normalizar(q).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var x = w;
                query = query.Where(p => p.TextoBusqueda.Contains(x));
            }
        var lista = await query.OrderBy(p => p.Estado == "E").ThenBy(p => p.Codigo).Take(15)
            .Select(p => new { p.Codigo, p.Descripcion, p.Estado }).ToListAsync();
        return new JsonResult(lista);
    }
}
