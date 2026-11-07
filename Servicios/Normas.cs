using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.AspNetCore.Html;
using Nomenclador.Web.Data;
using Nomenclador.Web.Data.Seed;

namespace Nomenclador.Web.Servicios;

/// <summary>Normas particulares, generales y bioquímicas del PDF (datos fijos de NormasSeed).</summary>
public static partial class Normas
{
    public record Enlace(string Ancla, string Codigo, string Titulo);

    public static IReadOnlyList<ParteNormaSeed> Partes => NormasSeed.Partes;
    public static IReadOnlyList<NormaSeed> Todas => NormasSeed.Normas;

    public static string Ancla(NormaSeed n) => "n-" + n.Clave;
    public static string Ancla(ParteNormaSeed p) => "p-" + p.Clave;

    /// <summary>Texto normalizado (sin acentos, minúsculas) para el buscador de la página de normas.</summary>
    public static string TextoBusqueda(NormaSeed n) => AppDb.Normalizar($"{n.Codigo} {n.Titulo} {n.Texto}");

    private static bool Aplica(string prefijo, string codigo) =>
        codigo.Equals(prefijo, StringComparison.OrdinalIgnoreCase) || codigo.StartsWith(prefijo + ".", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Normas que aplican a un código (grupo o práctica). Si se pasa <paramref name="excluirHasta"/>, se omiten
    /// las que ya aplican a ese código más corto (p.ej. las del grupo, para no repetirlas en cada práctica).
    /// Las partes con "aplica" (bioquímicas) se devuelven como un solo enlace.
    /// </summary>
    public static List<Enlace> Para(string codigo, string? excluirHasta = null)
    {
        bool Sirve(string[] prefijos) =>
            prefijos.Any(p => Aplica(p, codigo) && (excluirHasta is null || !Aplica(p, excluirHasta)));

        var r = new List<Enlace>();
        foreach (var p in Partes.Where(p => Sirve(p.Aplica)))
            r.Add(new Enlace(Ancla(p), "", Titulo(p.Nombre)));
        foreach (var n in Todas.Where(n => Sirve(n.Aplica)))
            r.Add(new Enlace(Ancla(n), n.Codigo, Titulo(n.Titulo)));
        return r;
    }

    /// <summary>"TERAPIA INTENSIVA" → "Terapia intensiva".</summary>
    public static string Titulo(string s) =>
        s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..].ToLowerInvariant();

    [GeneratedRegex(@"\*\*(.+?)\*\*")] private static partial Regex Negrita();
    [GeneratedRegex(@"(?<![\\\w])\*(?!\s)(.+?)(?<![\s\\])\*(?!\w)")] private static partial Regex Cursiva();

    /// <summary>Markdown mínimo: párrafos, listas "- ", **negrita**, *cursiva* (ampliaciones de la norma), "\*" literal.</summary>
    public static IHtmlContent Html(string texto)
    {
        var sb = new StringBuilder();
        foreach (var bloque in texto.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var lineas = bloque.Split('\n');
            if (lineas.All(l => l.StartsWith("- ")))
            {
                sb.Append("<ul>");
                foreach (var l in lineas) sb.Append("<li>").Append(Linea(l[2..])).Append("</li>");
                sb.Append("</ul>");
                continue;
            }
            var html = Linea(string.Join(' ', lineas));
            var ampliacion = bloque.StartsWith('*') && !bloque.StartsWith("**") && bloque.EndsWith('*');
            sb.Append(ampliacion ? "<p class=\"ampliacion\">" : "<p>").Append(html).Append("</p>");
        }
        return new HtmlString(sb.ToString());
    }

    private static readonly HtmlEncoder Codificador = HtmlEncoder.Create(UnicodeRanges.All);

    private static string Linea(string s)
    {
        var h = Codificador.Encode(s);
        h = Negrita().Replace(h, "<strong>$1</strong>");
        h = Cursiva().Replace(h, "<em>$1</em>");
        return h.Replace("\\*", "*");
    }
}
