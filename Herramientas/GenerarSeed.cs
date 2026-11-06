namespace Nomenclador.Web.Herramientas;

/// <summary>
/// dotnet run -- generar-seed [carpetaRaw] [archivoSalida]
/// Por defecto lee ../../data/raw y escribe Data/Seed/NomencladorSeed.cs.
/// </summary>
public static class GenerarSeed
{
    public static int Ejecutar(string[] args)
    {
        var proyecto = BuscarCarpetaProyecto();
        var raw = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(proyecto, "..", "..", "data", "raw"));
        var salida = args.Length > 1 ? args[1] : Path.Combine(proyecto, "Data", "Seed", "NomencladorSeed.cs");

        if (!Directory.Exists(raw))
        {
            Console.Error.WriteLine($"No existe la carpeta {raw}");
            return 1;
        }

        var r = RawParser.LeerCarpeta(raw);
        foreach (var a in r.Avisos) Console.WriteLine("AVISO: " + a);

        // Verificación: el total calculado con el juego I.N.O.S. 1991 debe coincidir con el total impreso en el PDF.
        var inos = Data.Coeficientes.Todos.ToDictionary(d => d.Clave, d => d.ValorInos1991);
        int verificadas = 0, difieren = 0;
        foreach (var p in r.Practicas)
        {
            if (!r.TotalesPdf.TryGetValue(p.Codigo, out var t)) continue;
            var (h, g) = Data.Coeficientes.PorDefectoParaSeccion(p.Codigo.Split('.')[0]);
            var vh = inos[h];
            var vg = inos[p.CoefGasto ?? g];
            var exacto = ((p.UEspecialista ?? 0) + (p.AyudantesCantidad ?? 1) * (p.UAyudante ?? 0) + (p.UAnestesista ?? 0)) * vh
                         + (p.UGasto ?? 0) * vg;
            verificadas++;
            if (Math.Abs(exacto - t.total) > 0.04m)
            {
                difieren++;
                Console.WriteLine($"REVISAR {t.ubic} {p.Codigo}: calculado {exacto:0.00} vs PDF {t.total:0.00}");
            }
        }
        if (verificadas > 0) Console.WriteLine($"Verificación contra totales del PDF: {verificadas - difieren}/{verificadas} OK");

        var cs = SeedWriter.Generar(r.Secciones, r.Grupos, r.Practicas, "data/raw/*.txt");
        File.WriteAllText(salida, cs, new System.Text.UTF8Encoding(true));
        Console.WriteLine($"OK: {r.Secciones.Count} secciones, {r.Grupos.Count} grupos, {r.Practicas.Count} prácticas -> {salida}");
        return 0;
    }

    private static string BuscarCarpetaProyecto()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Nomenclador.Web.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
