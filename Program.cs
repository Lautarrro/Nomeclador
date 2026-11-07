using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nomenclador.Web.Data;
using Nomenclador.Web.Data.Seed;
using Nomenclador.Web.Herramientas;
using Nomenclador.Web.Servicios;
using Microsoft.AspNetCore.HttpOverrides;

// Herramienta: regenerar Data/Seed/NomencladorSeed.cs desde data/raw/*.txt
if (args.Length > 0 && args[0] == "generar-seed")
{
    return GenerarSeed.Ejecutar(args.Skip(1).ToArray());
}

// Números con punto decimal en formularios y JSON, sin depender del idioma de Windows.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
var appData = Environment.GetEnvironmentVariable("APP_DATA_PATH")
              ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");

Directory.CreateDirectory(appData);

var clavesPath = Path.Combine(appData, "claves");
Directory.CreateDirectory(clavesPath);

var cadena = builder.Configuration.GetConnectionString("Nomenclador")
             ?? $"Data Source={Path.Combine(appData, "nomenclador.db")};Default Timeout=30;Cache=Shared";
// var appData = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
// Directory.CreateDirectory(appData);
// var cadena = builder.Configuration.GetConnectionString("Nomenclador")
//              ?? $"Data Source={Path.Combine(appData, "nomenclador.db")};Default Timeout=30;Cache=Shared";

// Acentos y ñ sin escapar en el HTML generado.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(cadena));
builder.Services.AddScoped<Calculadora>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ServicioUsuarios>();

// Las claves de cookies se guardan en disco: las sesiones sobreviven a reinicios del servidor.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(clavesPath))
    .SetApplicationName("Nomenclador");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Ingresar";
        o.LogoutPath = "/Cuenta/Salir";
        o.AccessDeniedPath = "/Cuenta/SinPermiso";
        o.ExpireTimeSpan = TimeSpan.FromHours(10);
        o.SlidingExpiration = true;
        o.Cookie.Name = "nomenclador.sesion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        // Si el admin desactiva un usuario o le cambia la clave/rol, la sesión se corta.
        o.Events.OnValidatePrincipal = ServicioUsuarios.ValidarSesionAsync;
    });

builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build();
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "x",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Admin", "Admin");
    o.Conventions.AllowAnonymousToPage("/Cuenta/Ingresar");
    o.Conventions.AllowAnonymousToPage("/Error");
});
builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", p => p.RequireRole(Roles.Admin));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // La aplicación estará detrás del proxy de Render.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
var app = builder.Build();
app.UseForwardedHeaders();
await Inicializador.EjecutarAsync(app.Services, app.Logger);

// Herramienta: dotnet run -- resetear-admin [usuario]
// Genera una clave temporal nueva (se pide cambiarla al ingresar) y la muestra en la consola.
if (args.Length > 0 && args[0] == "resetear-admin")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    var usuarios = scope.ServiceProvider.GetRequiredService<ServicioUsuarios>();
    var nombre = (args.Length > 1 ? args[1] : "admin").Trim().ToLower();
    var u = await db.Usuarios.FirstOrDefaultAsync(x => x.NombreUsuario == nombre);
    if (u is null)
    {
        Console.Error.WriteLine($"No existe el usuario '{nombre}'.");
        return 1;
    }
    var clave = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(9)).Replace('/', 'x').Replace('+', 'y');
    usuarios.FijarClave(u, clave, temporal: true);
    u.Activo = true;
    await db.SaveChangesAsync();
    Console.WriteLine($"=== Nueva clave temporal: {u.NombreUsuario} / {clave}  (se pide cambiarla al ingresar) ===");
    return 0;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (ctx, next) =>
{
    // Usuario con clave temporal: solo puede ir a cambiarla.
    if (ctx.User.Identity?.IsAuthenticated == true && ctx.User.HasClaim("debe_cambiar_clave", "1"))
    {
        var path = ctx.Request.Path.Value ?? "";
        if (!path.StartsWith("/Cuenta", StringComparison.OrdinalIgnoreCase) && !Path.HasExtension(path))
        {
            ctx.Response.Redirect("/Cuenta/CambiarClave");
            return;
        }
    }
    await next();
});

app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages().WithStaticAssets();

app.Run();
return 0;
