namespace Nomenclador.Web.Data.Seed;

public record SeccionSeed(string Codigo, string Nombre);

public record GrupoSeed(string Codigo, string SeccionCodigo, string Nombre, string? Nota = null);

public record PracticaSeed(
    string Codigo,
    string Estado,
    string Descripcion,
    decimal? UEspecialista,
    int? AyudantesCantidad,
    decimal? UAyudante,
    decimal? UAnestesista,
    decimal? UGasto,
    string? CoefGasto,
    string? Nota);
