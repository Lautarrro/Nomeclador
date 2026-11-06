using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;

namespace Nomenclador.Web.Servicios;

public record LineaPedido(
    string Codigo,
    decimal Porcentaje = 100,
    int Cantidad = 1,
    bool Ayudantes = true,
    bool Anestesia = true,
    bool Gastos = true);

public record Rubro(string Concepto, decimal Unidades, string Coeficiente, decimal ValorUnidad, decimal Importe);

public record LineaResultado(
    string Codigo,
    string Descripcion,
    string Estado,
    decimal Porcentaje,
    int Cantidad,
    List<Rubro> Rubros,
    decimal Subtotal,
    decimal Total,
    string? Aviso);

public record ResultadoCalculo(
    int JuegoId,
    string JuegoNombre,
    List<LineaResultado> Lineas,
    decimal TotalHonorarios,
    decimal TotalGastos,
    decimal Total,
    List<string> NoEncontrados);

public class Calculadora(AppDb db)
{
    /// <summary>Total al 100% de una práctica (para listados). Requiere Grupo.Seccion cargados.</summary>
    public static decimal? TotalRapido(Practica p, IReadOnlyDictionary<string, decimal> valores)
    {
        if (!p.TieneValores) return null;
        var vHon = valores.GetValueOrDefault(p.Grupo?.Seccion?.CoefHonorario ?? Coeficientes.Galeno);
        var vGas = valores.GetValueOrDefault(p.CoefGastoOverride ?? p.Grupo?.Seccion?.CoefGasto ?? Coeficientes.OtrosGastos);
        decimal R(decimal? u, decimal v) => Math.Round((u ?? 0) * v, 2, MidpointRounding.AwayFromZero);
        return R(p.UEspecialista, vHon)
             + R((p.UAyudante ?? 0) * Math.Max(p.AyudantesCantidad ?? 1, 1), vHon)
             + R(p.UAnestesista, vHon)
             + R(p.UGasto, vGas);
    }

    public async Task<(JuegoValores? juego, Dictionary<string, decimal> valores)> JuegoAsync(int? juegoId)
    {
        var juego = juegoId is int id
            ? await db.JuegosValores.AsNoTracking().Include(j => j.Valores).FirstOrDefaultAsync(j => j.Id == id && j.Activo)
            : null;
        juego ??= await db.JuegosValores.AsNoTracking().Include(j => j.Valores)
            .OrderByDescending(j => j.PorDefecto).ThenBy(j => j.Id).FirstOrDefaultAsync(j => j.Activo);
        return (juego, juego?.Valores.ToDictionary(v => v.Clave, v => v.Valor) ?? new());
    }

    public async Task<ResultadoCalculo?> CalcularAsync(int? juegoId, IReadOnlyList<LineaPedido> pedido)
    {
        var (juego, valores) = await JuegoAsync(juegoId);
        if (juego is null) return null;

        var codigos = pedido.Select(l => l.Codigo).Distinct().ToList();
        var practicas = await db.Practicas.AsNoTracking()
            .Include(p => p.Grupo!).ThenInclude(g => g.Seccion)
            .Where(p => codigos.Contains(p.Codigo))
            .ToDictionaryAsync(p => p.Codigo);

        var lineas = new List<LineaResultado>();
        var noEncontrados = new List<string>();
        decimal totHon = 0, totGas = 0;

        foreach (var l in pedido)
        {
            if (!practicas.TryGetValue(l.Codigo, out var p)) { noEncontrados.Add(l.Codigo); continue; }

            var claveHon = p.Grupo?.Seccion?.CoefHonorario ?? Coeficientes.Galeno;
            var claveGas = p.CoefGastoOverride ?? p.Grupo?.Seccion?.CoefGasto ?? Coeficientes.OtrosGastos;
            var vHon = valores.GetValueOrDefault(claveHon);
            var vGas = valores.GetValueOrDefault(claveGas);
            var factor = Math.Clamp(l.Porcentaje, 0, 1000) / 100m * Math.Max(l.Cantidad, 1);

            var rubros = new List<Rubro>();
            decimal hon = 0, gas = 0;
            void Agregar(string concepto, decimal? u, string clave, decimal v, bool esGasto)
            {
                if (u is null or 0) return;
                var imp = Math.Round(u.Value * v, 2, MidpointRounding.AwayFromZero);
                rubros.Add(new Rubro(concepto, u.Value, Coeficientes.NombreDe(clave), v, imp));
                if (esGasto) gas += imp; else hon += imp;
            }

            Agregar("Especialista", p.UEspecialista, claveHon, vHon, false);
            if (l.Ayudantes && p.UAyudante is > 0)
            {
                var cant = Math.Max(p.AyudantesCantidad ?? 1, 1);
                Agregar(cant == 1 ? "Ayudante" : $"Ayudantes ({cant} × {p.UAyudante:0.##} U)", p.UAyudante * cant, claveHon, vHon, false);
            }
            if (l.Anestesia) Agregar("Anestesista", p.UAnestesista, claveHon, vHon, false);
            if (l.Gastos) Agregar(Coeficientes.NombreDe(claveGas), p.UGasto, claveGas, vGas, true);

            var subtotal = hon + gas;
            var honF = Math.Round(hon * factor, 2, MidpointRounding.AwayFromZero);
            var gasF = Math.Round(gas * factor, 2, MidpointRounding.AwayFromZero);
            var total = honF + gasF;
            totHon += honF;
            totGas += gasF;

            string? aviso = null;
            if (p.Estado == "E") aviso = "Código ELIMINADO del nomenclador.";
            else if (!p.TieneValores) aviso = "Sin unidades en el nomenclador (código P.M.O. o valor a convenir).";

            lineas.Add(new LineaResultado(p.Codigo, p.Descripcion, p.Estado, l.Porcentaje, Math.Max(l.Cantidad, 1),
                rubros, subtotal, total, aviso));
        }

        return new ResultadoCalculo(juego.Id, juego.Nombre, lineas, totHon, totGas,
            lineas.Sum(x => x.Total), noEncontrados);
    }
}
