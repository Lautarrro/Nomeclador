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

/// <summary>Parte de las normas (Particulares, Generales, Bioquímicas). Aplica = prefijos de código a los que remite como conjunto.</summary>
public record ParteNormaSeed(string Clave, string Nombre, string[] Aplica);

/// <summary>Norma del PDF (págs. 159–167). Texto en markdown simple; Aplica = prefijos de código ("34", "01", "10.01.06").</summary>
public record NormaSeed(string Clave, string ParteClave, string Codigo, string Titulo, int Pagina, string[] Aplica, string Texto);
