using System.ComponentModel.DataAnnotations;

namespace Nomenclador.Web.Data;

/// <summary>Capítulo del nomenclador (ej. "01 - Operaciones en el sistema nervioso").</summary>
public class Seccion
{
    [Key, MaxLength(4)]
    public string Codigo { get; set; } = "";
    [MaxLength(300)]
    public string Nombre { get; set; } = "";
    /// <summary>Clave del coeficiente de honorarios (ej. GALENO).</summary>
    [MaxLength(40)]
    public string CoefHonorario { get; set; } = Coeficientes.Galeno;
    /// <summary>Clave del coeficiente de gastos (ej. GASTO_QUIRURGICO).</summary>
    [MaxLength(40)]
    public string CoefGasto { get; set; } = Coeficientes.GastoQuirurgico;
    public List<Grupo> Grupos { get; set; } = new();
}

/// <summary>Grupo dentro de un capítulo (ej. "01.01 - Operaciones en el cráneo").</summary>
public class Grupo
{
    [Key, MaxLength(8)]
    public string Codigo { get; set; } = "";
    [MaxLength(4)]
    public string SeccionCodigo { get; set; } = "";
    public Seccion? Seccion { get; set; }
    [MaxLength(300)]
    public string Nombre { get; set; } = "";
    /// <summary>Norma que aplica a todas las prácticas del grupo.</summary>
    [MaxLength(4000)]
    public string? Nota { get; set; }
    public List<Practica> Practicas { get; set; } = new();
}

public class Practica
{
    public int Id { get; set; }
    [Required, MaxLength(12)]
    public string Codigo { get; set; } = "";
    [MaxLength(8)]
    public string GrupoCodigo { get; set; } = "";
    public Grupo? Grupo { get; set; }
    /// <summary>"" = original, N = nuevo (P.M.O.), E = eliminado.</summary>
    [MaxLength(1)]
    public string Estado { get; set; } = "";
    [Required, MaxLength(2000)]
    public string Descripcion { get; set; } = "";
    /// <summary>Unidades de honorarios del especialista (o bioquímico).</summary>
    public decimal? UEspecialista { get; set; }
    public int? AyudantesCantidad { get; set; }
    /// <summary>Unidades por cada ayudante.</summary>
    public decimal? UAyudante { get; set; }
    public decimal? UAnestesista { get; set; }
    public decimal? UGasto { get; set; }
    /// <summary>Si se completa, reemplaza el coeficiente de gastos de la sección.</summary>
    [MaxLength(40)]
    public string? CoefGastoOverride { get; set; }
    [MaxLength(4000)]
    public string? Nota { get; set; }
    /// <summary>Texto normalizado (sin acentos, minúsculas) para búsqueda.</summary>
    [MaxLength(6000)]
    public string TextoBusqueda { get; set; } = "";
    /// <summary>True si la creó o modificó un admin (la carga inicial no la pisa).</summary>
    public bool EditadaPorAdmin { get; set; }
    public DateTime ActualizadoUtc { get; set; } = DateTime.UtcNow;
    [ConcurrencyCheck]
    public int Version { get; set; }

    public bool TieneValores => UEspecialista is not null || UAnestesista is not null || UGasto is not null || UAyudante is not null;
}

/// <summary>Juego de valores: conjunto de coeficientes ($ por unidad). Puede haber uno por convenio/financiador.</summary>
public class JuegoValores
{
    public int Id { get; set; }
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = "";
    [MaxLength(500)]
    public string? Descripcion { get; set; }
    public bool PorDefecto { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime ActualizadoUtc { get; set; } = DateTime.UtcNow;
    public List<ValorCoeficiente> Valores { get; set; } = new();
}

public class ValorCoeficiente
{
    public int Id { get; set; }
    public int JuegoValoresId { get; set; }
    public JuegoValores? JuegoValores { get; set; }
    [Required, MaxLength(40)]
    public string Clave { get; set; } = "";
    public decimal Valor { get; set; }
}

public class Usuario
{
    public int Id { get; set; }
    [Required, MaxLength(60)]
    public string NombreUsuario { get; set; } = "";
    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = "";
    public string ClaveHash { get; set; } = "";
    [MaxLength(20)]
    public string Rol { get; set; } = Roles.Usuario;
    public bool Activo { get; set; } = true;
    public bool DebeCambiarClave { get; set; }
    /// <summary>Se cambia al cambiar clave/rol/estado para invalidar sesiones abiertas.</summary>
    [MaxLength(40)]
    public string SelloSeguridad { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoIngresoUtc { get; set; }
}

public class TemaForo
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = "";
    [Required, MaxLength(20000)]
    public string Cuerpo { get; set; } = "";
    [MaxLength(40)]
    public string Categoria { get; set; } = "Consultas";
    /// <summary>Código de práctica relacionado (opcional).</summary>
    [MaxLength(12)]
    public string? CodigoPractica { get; set; }
    public bool Fijado { get; set; }
    public bool Cerrado { get; set; }
    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime UltimaActividadUtc { get; set; } = DateTime.UtcNow;
    public List<RespuestaForo> Respuestas { get; set; } = new();
}

public class RespuestaForo
{
    public int Id { get; set; }
    public int TemaForoId { get; set; }
    public TemaForo? Tema { get; set; }
    [Required, MaxLength(20000)]
    public string Cuerpo { get; set; } = "";
    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Usuario = "Usuario";
}

public static class CategoriasForo
{
    public static readonly string[] Todas = { "Novedades", "Normas", "Consultas", "Facturación", "General" };
}

/// <summary>Claves de coeficientes del Juego de Valores (I.N.O.S. 1991 como referencia).</summary>
public static class Coeficientes
{
    public const string Galeno = "GALENO";
    public const string HonorarioOdontologico = "HON_ODONTO";
    public const string HonorarioBioquimico = "HON_BIOQ";
    public const string GastoQuirurgico = "GASTO_QUIRURGICO";
    public const string GastoRadiologico = "GASTO_RADIOLOGICO";
    public const string PensionSanatorial = "PENSION_SANATORIAL";
    public const string GastoBioquimico = "GASTO_BIOQ";
    public const string GastoOdontologico = "GASTO_ODONTO";
    public const string GastoProtesis = "GASTO_PROTESIS";
    public const string OtrosGastos = "OTROS_GASTOS";

    public record Def(string Clave, string Nombre, bool EsHonorario, decimal ValorInos1991, string Aplicacion);

    public static readonly Def[] Todos =
    {
        new(Galeno, "Galeno", true, 0.2326m, "Honorarios médicos generales"),
        new(HonorarioOdontologico, "Honorario odontológico", true, 0.1123m, "Odontología (nomenclador odontológico ON)"),
        new(HonorarioBioquimico, "Honorario bioquímico", true, 0.1315m, "Prestaciones bioquímicas"),
        new(GastoQuirurgico, "Gasto quirúrgico", false, 0.4927m, "Cirugías (capítulos 01 a 13)"),
        new(GastoRadiologico, "Gasto radiológico", false, 0.3496m, "Radiología (capítulos 34 y 35)"),
        new(PensionSanatorial, "Pensión sanatorial", false, 0.5634m, "Hotelería hospitalaria (capítulos 40, 41 y 43)"),
        new(GastoBioquimico, "Gasto bioquímico", false, 0.9702m, "Prestaciones bioquímicas (capítulo 66)"),
        new(GastoOdontologico, "Gasto odontológico", false, 0.1715m, "Odontología (nomenclador odontológico ON)"),
        new(GastoProtesis, "Gasto odont. prótesis", false, 0.2528m, "Prótesis odontológicas"),
        new(OtrosGastos, "Otros gastos", false, 0.1226m, "Estudios especializados (capítulos 14 a 43)"),
    };

    public static Def? Buscar(string clave) => Todos.FirstOrDefault(d => d.Clave == clave);
    public static string NombreDe(string clave) => Buscar(clave)?.Nombre ?? clave;

    /// <summary>Coeficientes por defecto según el número de capítulo (pág. 5 del PDF).</summary>
    public static (string honorario, string gasto) PorDefectoParaSeccion(string codigoSeccion)
    {
        if (codigoSeccion == "ON") return (HonorarioOdontologico, GastoOdontologico);
        if (!int.TryParse(codigoSeccion, out var n)) return (Galeno, OtrosGastos);
        return n switch
        {
            >= 1 and <= 13 => (Galeno, GastoQuirurgico),
            34 or 35 => (Galeno, GastoRadiologico),
            40 or 41 or 43 => (Galeno, PensionSanatorial),
            15 => (Galeno, GastoBioquimico),
            16 => (Galeno, GastoQuirurgico),
            18 => (Galeno, GastoRadiologico),
            23 => (Galeno, GastoBioquimico),
            >= 14 and <= 33 => (Galeno, OtrosGastos),
            66 => (HonorarioBioquimico, GastoBioquimico),
            _ => (Galeno, OtrosGastos),
        };
    }
}
