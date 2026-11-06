using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Nomenclador.Web.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Seccion> Secciones => Set<Seccion>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Practica> Practicas => Set<Practica>();
    public DbSet<JuegoValores> JuegosValores => Set<JuegoValores>();
    public DbSet<ValorCoeficiente> ValoresCoeficiente => Set<ValorCoeficiente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TemaForo> Temas => Set<TemaForo>();
    public DbSet<RespuestaForo> Respuestas => Set<RespuestaForo>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Practica>().HasIndex(p => p.Codigo).IsUnique();
        b.Entity<Practica>().HasIndex(p => p.GrupoCodigo);
        b.Entity<Practica>().HasOne(p => p.Grupo).WithMany(g => g.Practicas)
            .HasForeignKey(p => p.GrupoCodigo).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Grupo>().HasOne(g => g.Seccion).WithMany(s => s.Grupos)
            .HasForeignKey(g => g.SeccionCodigo).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ValorCoeficiente>().HasIndex(v => new { v.JuegoValoresId, v.Clave }).IsUnique();
        b.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        b.Entity<TemaForo>().HasIndex(t => t.UltimaActividadUtc);
        b.Entity<TemaForo>().HasOne(t => t.Autor).WithMany().HasForeignKey(t => t.AutorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RespuestaForo>().HasOne(r => r.Autor).WithMany().HasForeignKey(r => r.AutorId).OnDelete(DeleteBehavior.Restrict);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Preparar();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        Preparar();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    /// <summary>Mantiene el texto de búsqueda y la versión de concurrencia de las prácticas.</summary>
    private void Preparar()
    {
        foreach (var e in ChangeTracker.Entries<Practica>())
        {
            if (e.State is EntityState.Added or EntityState.Modified)
            {
                var p = e.Entity;
                p.TextoBusqueda = Normalizar($"{p.Codigo} {p.Descripcion} {p.Nota}");
                p.ActualizadoUtc = DateTime.UtcNow;
                if (e.State == EntityState.Modified) p.Version++;
            }
        }
    }

    /// <summary>Minúsculas y sin acentos, para que "cesarea" encuentre "Cesárea".</summary>
    public static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
