using System.Text;
using System.Text.RegularExpressions;
using Nomenclador.Web.Data.Seed;

namespace Nomenclador.Web.Herramientas;

/// <summary>
/// Lee data/normas/*.md y genera Data/Seed/NormasSeed.cs.
/// "# PARTE" abre una parte; "## CODIGO xx | Título" (o "## NORMA BIOQ 001 | Título") abre una norma;
/// "&lt;!-- aplica: 01-13, 42.03 --&gt;" debajo de un título = prefijos de código a los que aplica;
/// "&lt;!-- p.NNN --&gt;" = página del PDF; cualquier otro comentario de una línea se ignora.
/// </summary>
public static partial class NormasParser
{
    [GeneratedRegex(@"^<!--\s*p\.(\d+)\s*-->$")] private static partial Regex Pagina();
    [GeneratedRegex(@"^<!--\s*aplica:\s*(.*?)\s*-->$")] private static partial Regex Aplica();

    public static (List<ParteNormaSeed> Partes, List<NormaSeed> Normas, List<string> Avisos) Leer(string carpeta)
    {
        var partes = new List<ParteNormaSeed>();
        var normas = new List<NormaSeed>();
        var avisos = new List<string>();
        int pagina = 0;
        ParteNormaSeed? parte = null;
        string? codigo = null, titulo = null, clave = null;
        int paginaNorma = 0;
        string[] aplica = [];
        var texto = new StringBuilder();

        void CerrarNorma()
        {
            if (codigo is null) return;
            normas.Add(new NormaSeed(clave!, parte?.Clave ?? "", codigo, titulo!, paginaNorma, aplica, texto.ToString().Trim()));
            codigo = null; aplica = []; texto.Clear();
        }

        foreach (var archivo in Directory.GetFiles(carpeta, "*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            foreach (var linea in File.ReadLines(archivo))
            {
                var l = linea.TrimEnd();
                if (Pagina().Match(l) is { Success: true } mp) { pagina = int.Parse(mp.Groups[1].Value); continue; }
                if (Aplica().Match(l) is { Success: true } ma)
                {
                    var prefijos = Expandir(ma.Groups[1].Value);
                    if (codigo is not null) aplica = prefijos;
                    else if (parte is not null) parte = partes[^1] = parte with { Aplica = prefijos };
                    continue;
                }
                if (l.StartsWith("<!--")) continue;
                if (l.StartsWith("# "))
                {
                    CerrarNorma();
                    var nombre = l[2..].Trim();
                    parte = new ParteNormaSeed(Clave(nombre), nombre, []);
                    partes.Add(parte);
                    continue;
                }
                if (l.StartsWith("## "))
                {
                    CerrarNorma();
                    var c = l[3..].Split('|', 2);
                    if (c.Length < 2) { avisos.Add($"Título de norma sin '|': {l}"); continue; }
                    codigo = c[0].Trim();
                    foreach (var pre in new[] { "CODIGO ", "CÓDIGO " })
                        if (codigo.StartsWith(pre, StringComparison.OrdinalIgnoreCase)) codigo = codigo[pre.Length..].Trim();
                    titulo = c[1].Trim();
                    clave = Clave(codigo);
                    paginaNorma = pagina;
                    if (normas.Any(n => n.Clave == clave)) avisos.Add($"Norma repetida: {codigo}");
                    continue;
                }
                if (codigo is not null) texto.AppendLine(l);
                else if (l.Trim().Length > 0 && parte is not null) avisos.Add($"Texto fuera de una norma ({parte.Nombre}): {l[..Math.Min(60, l.Length)]}");
            }
            CerrarNorma();
        }

        // Partes sin normas (p.ej. el título del documento) no se publican.
        partes.RemoveAll(p => !normas.Any(n => n.ParteClave == p.Clave));
        return (partes, normas, avisos);
    }

    /// <summary>"01-13, 42.03" → ["01".."13", "42.03"].</summary>
    private static string[] Expandir(string s)
    {
        var r = new List<string>();
        foreach (var parte in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var rango = parte.Split('-');
            if (rango.Length == 2 && int.TryParse(rango[0], out var a) && int.TryParse(rango[1], out var b))
                for (var i = a; i <= b; i++) r.Add(i.ToString("00"));
            else r.Add(parte);
        }
        return r.ToArray();
    }

    private static string Clave(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in Data.AppDb.Normalizar(s))
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        return Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
    }

    public static string GenerarCs(List<ParteNormaSeed> partes, List<NormaSeed> normas)
    {
        static string Lit(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\"";
        static string Arr(string[] a) => a.Length == 0 ? "[]" : "[" + string.Join(", ", a.Select(Lit)) + "]";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated> Generado por `dotnet run -- generar-seed` desde data/normas/*.md. No editar a mano. </auto-generated>");
        sb.AppendLine("namespace Nomenclador.Web.Data.Seed;");
        sb.AppendLine();
        sb.AppendLine("public static class NormasSeed");
        sb.AppendLine("{");
        sb.AppendLine("    public static readonly ParteNormaSeed[] Partes =");
        sb.AppendLine("    [");
        foreach (var p in partes) sb.AppendLine($"        new({Lit(p.Clave)}, {Lit(p.Nombre)}, {Arr(p.Aplica)}),");
        sb.AppendLine("    ];");
        sb.AppendLine();
        sb.AppendLine("    public static readonly NormaSeed[] Normas =");
        sb.AppendLine("    [");
        foreach (var n in normas)
            sb.AppendLine($"        new({Lit(n.Clave)}, {Lit(n.ParteClave)}, {Lit(n.Codigo)}, {Lit(n.Titulo)}, {n.Pagina}, {Arr(n.Aplica)},\n            {Lit(n.Texto)}),");
        sb.AppendLine("    ];");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
