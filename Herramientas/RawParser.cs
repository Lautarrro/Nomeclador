using System.Globalization;
using Nomenclador.Web.Data.Seed;

namespace Nomenclador.Web.Herramientas;

/// <summary>
/// Lee los archivos de transcripción data/raw/*.txt.
/// Formato: codigo|flag|descripcion|especialista|ayudCant|ayudU|anestesista|gasto|nota[|coefGasto[|totalPdf]]
/// Líneas "#S|01|Nombre" = sección, "#G|01.01|Nombre" = grupo, otras "#" = comentario.
/// </summary>
public static class RawParser
{
    public record Resultado(List<SeccionSeed> Secciones, List<GrupoSeed> Grupos, List<PracticaSeed> Practicas, List<string> Avisos, Dictionary<string, (decimal total, string ubic)> TotalesPdf);

    public static Resultado LeerCarpeta(string carpeta)
    {
        var secciones = new Dictionary<string, SeccionSeed>();
        var grupos = new Dictionary<string, GrupoSeed>();
        var practicas = new Dictionary<string, PracticaSeed>();
        var totales = new Dictionary<string, (decimal, string)>();
        var avisos = new List<string>();

        foreach (var archivo in Directory.GetFiles(carpeta, "*.txt").OrderBy(f => f, StringComparer.Ordinal))
        {
            var nombre = Path.GetFileName(archivo);
            var nro = 0;
            foreach (var linea in File.ReadLines(archivo))
            {
                nro++;
                var l = linea.Trim();
                if (l.Length == 0) continue;
                var c = l.Split('|');
                string Ubic() => $"{nombre}:{nro}";

                if (l.StartsWith("#S|"))
                {
                    if (c.Length < 3) { avisos.Add($"{Ubic()} sección mal formada"); continue; }
                    secciones[c[1].Trim()] = new SeccionSeed(c[1].Trim(), c[2].Trim());
                    continue;
                }
                if (l.StartsWith("#G|"))
                {
                    if (c.Length < 3) { avisos.Add($"{Ubic()} grupo mal formado"); continue; }
                    var cod = c[1].Trim();
                    grupos[cod] = new GrupoSeed(cod, cod.Split('.')[0], c[2].Trim());
                    continue;
                }
                if (l.StartsWith("#N|"))
                {
                    // Norma de grupo: "#N|08.05|texto" (varias líneas se concatenan).
                    if (c.Length < 3 || !grupos.TryGetValue(c[1].Trim(), out var gr)) { avisos.Add($"{Ubic()} norma de grupo sin #G previo"); continue; }
                    var texto = string.Join('|', c.Skip(2)).Trim();
                    grupos[gr.Codigo] = gr with { Nota = gr.Nota is null ? texto : gr.Nota + " " + texto };
                    continue;
                }
                if (l.StartsWith('#')) continue;

                if (c.Length < 9) { avisos.Add($"{Ubic()} tiene {c.Length} columnas (se esperan 9)"); continue; }
                if (c.Length > 11) avisos.Add($"{Ubic()} tiene {c.Length} columnas (máximo 11): revisar separadores");
                if (c.Length > 9 && c[9].Trim().Length > 0 && Data.Coeficientes.Buscar(c[9].Trim()) is null)
                    avisos.Add($"{Ubic()} coeficiente de gasto desconocido '{c[9].Trim()}'");
                var codigo = c[0].Trim();
                var partes = codigo.Split('.');
                if (partes.Length < 2) { avisos.Add($"{Ubic()} código inválido '{codigo}'"); continue; }

                var p = new PracticaSeed(
                    codigo,
                    c[1].Trim().ToUpperInvariant(),
                    c[2].Trim(),
                    Dec(c[3], Ubic, avisos),
                    Ent(c[4], Ubic, avisos),
                    Dec(c[5], Ubic, avisos),
                    Dec(c[6], Ubic, avisos),
                    Dec(c[7], Ubic, avisos),
                    c.Length > 9 && c[9].Trim().Length > 0 ? c[9].Trim() : null,
                    c[8].Trim().Length > 0 ? c[8].Trim() : null);

                if (practicas.ContainsKey(codigo)) avisos.Add($"{Ubic()} código repetido {codigo} (queda el último)");
                practicas[codigo] = p;
                if (c.Length > 10 && Dec(c[10], Ubic, avisos) is decimal tot) totales[codigo] = (tot, Ubic());

                var grupoCod = string.Join('.', partes.Take(partes.Length - 1));
                if (!grupos.ContainsKey(grupoCod))
                {
                    avisos.Add($"{Ubic()} grupo {grupoCod} sin línea #G, se crea vacío");
                    grupos[grupoCod] = new GrupoSeed(grupoCod, partes[0], "");
                }
            }
        }

        foreach (var g in grupos.Values.Where(g => !secciones.ContainsKey(g.SeccionCodigo)).ToList())
        {
            avisos.Add($"Sección {g.SeccionCodigo} sin línea #S, se crea vacía");
            secciones[g.SeccionCodigo] = new SeccionSeed(g.SeccionCodigo, "");
        }

        return new Resultado(
            secciones.Values.OrderBy(s => s.Codigo, StringComparer.Ordinal).ToList(),
            grupos.Values.OrderBy(g => g.Codigo, StringComparer.Ordinal).ToList(),
            practicas.Values.OrderBy(p => p.Codigo, StringComparer.Ordinal).ToList(),
            avisos,
            totales);
    }

    private static decimal? Dec(string s, Func<string> ubic, List<string> avisos)
    {
        s = s.Trim();
        if (s.Length == 0) return null;
        if (decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
        avisos.Add($"{ubic()} número inválido '{s}'");
        return null;
    }

    private static int? Ent(string s, Func<string> ubic, List<string> avisos)
    {
        s = s.Trim();
        if (s.Length == 0) return null;
        if (int.TryParse(s, out var v)) return v;
        avisos.Add($"{ubic()} cantidad de ayudantes inválida '{s}'");
        return null;
    }
}
