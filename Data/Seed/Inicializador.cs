using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Nomenclador.Web.Data.Seed;

public static class Inicializador
{
    public static async Task EjecutarAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        await db.Database.MigrateAsync();
        // WAL: lectores y escritor concurrentes sin bloquearse entre sí.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

        await CargarJuegoValoresAsync(db);
        var r = await CargarNomencladorAsync(db);
        if (r.nuevas + r.actualizadas > 0)
            logger.LogInformation("Nomenclador: {Nuevas} prácticas nuevas, {Act} actualizadas desde NomencladorSeed.cs", r.nuevas, r.actualizadas);

        await CrearAdminInicialAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>(), logger);
    }

    private static async Task CargarJuegoValoresAsync(AppDb db)
    {
        if (await db.JuegosValores.AnyAsync()) return;
        var j = new JuegoValores
        {
            Nombre = "I.N.O.S. marzo 1991",
            Descripcion = "Valores oficiales de referencia. Crear un juego nuevo con los valores vigentes de cada convenio.",
            PorDefecto = true,
        };
        foreach (var d in Coeficientes.Todos)
            j.Valores.Add(new ValorCoeficiente { Clave = d.Clave, Valor = d.ValorInos1991 });
        db.JuegosValores.Add(j);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Inserta lo que falte del seed y actualiza lo que no fue tocado por un admin.
    /// Nunca borra nada ni pisa cambios hechos desde el panel de administración.
    /// </summary>
    public static async Task<(int nuevas, int actualizadas)> CargarNomencladorAsync(AppDb db)
    {
        var secciones = await db.Secciones.ToDictionaryAsync(s => s.Codigo);
        foreach (var s in NomencladorSeed.Secciones)
        {
            if (secciones.TryGetValue(s.Codigo, out var ex))
            {
                if (string.IsNullOrEmpty(ex.Nombre) && s.Nombre.Length > 0) ex.Nombre = s.Nombre;
                continue;
            }
            var (h, g) = Coeficientes.PorDefectoParaSeccion(s.Codigo);
            var nueva = new Seccion { Codigo = s.Codigo, Nombre = s.Nombre, CoefHonorario = h, CoefGasto = g };
            db.Secciones.Add(nueva);
            secciones[s.Codigo] = nueva;
        }

        var grupos = await db.Grupos.ToDictionaryAsync(g => g.Codigo);
        foreach (var g in NomencladorSeed.Grupos)
        {
            if (grupos.TryGetValue(g.Codigo, out var ex))
            {
                if (string.IsNullOrEmpty(ex.Nombre) && g.Nombre.Length > 0) ex.Nombre = g.Nombre;
                if (string.IsNullOrEmpty(ex.Nota) && !string.IsNullOrEmpty(g.Nota)) ex.Nota = g.Nota;
                continue;
            }
            db.Grupos.Add(new Grupo { Codigo = g.Codigo, SeccionCodigo = g.SeccionCodigo, Nombre = g.Nombre, Nota = g.Nota });
        }

        var practicas = await db.Practicas.ToDictionaryAsync(p => p.Codigo);
        int nuevas = 0, actualizadas = 0;
        foreach (var s in NomencladorSeed.Practicas)
        {
            var grupo = string.Join('.', s.Codigo.Split('.').SkipLast(1));
            if (!practicas.TryGetValue(s.Codigo, out var p))
            {
                p = new Practica { Codigo = s.Codigo };
                Aplicar(p, s, grupo);
                db.Practicas.Add(p);
                nuevas++;
            }
            else if (!p.EditadaPorAdmin && Distinta(p, s, grupo))
            {
                Aplicar(p, s, grupo);
                actualizadas++;
            }
        }
        await db.SaveChangesAsync();
        return (nuevas, actualizadas);
    }

    private static void Aplicar(Practica p, PracticaSeed s, string grupo)
    {
        p.GrupoCodigo = grupo;
        p.Estado = s.Estado;
        p.Descripcion = s.Descripcion;
        p.UEspecialista = s.UEspecialista;
        p.AyudantesCantidad = s.AyudantesCantidad;
        p.UAyudante = s.UAyudante;
        p.UAnestesista = s.UAnestesista;
        p.UGasto = s.UGasto;
        p.CoefGastoOverride = s.CoefGasto;
        p.Nota = s.Nota;
    }

    private static bool Distinta(Practica p, PracticaSeed s, string grupo) =>
        p.GrupoCodigo != grupo || p.Estado != s.Estado || p.Descripcion != s.Descripcion ||
        p.UEspecialista != s.UEspecialista || p.AyudantesCantidad != s.AyudantesCantidad ||
        p.UAyudante != s.UAyudante || p.UAnestesista != s.UAnestesista || p.UGasto != s.UGasto ||
        p.CoefGastoOverride != s.CoefGasto || p.Nota != s.Nota;

    private static async Task CrearAdminInicialAsync(AppDb db, IPasswordHasher<Usuario> hasher, ILogger logger)
    {
        if (await db.Usuarios.AnyAsync()) return;
        var clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)).Replace('/', 'x').Replace('+', 'y');
        var admin = new Usuario
        {
            NombreUsuario = "admin",
            NombreCompleto = "Administrador",
            Rol = Roles.Admin,
            DebeCambiarClave = true,
        };
        admin.ClaveHash = hasher.HashPassword(admin, clave);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        logger.LogWarning("=== Usuario inicial creado: admin / {Clave}  (se pide cambiarla al ingresar) ===", clave);
    }
}
