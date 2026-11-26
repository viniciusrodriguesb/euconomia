using Infraestrutura;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Configuracao;

internal static class SegurancaExtensions
{
    public static IServiceCollection AdicionarSeguranca(this IServiceCollection servicos, IWebHostEnvironment ambiente, IConfiguration configuracao)
    {
        var politicaCookie = ambiente.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        var protecao = servicos.AddDataProtection().SetApplicationName("Euconomia");
        var diretorioChaves = configuracao["Seguranca:DiretorioChaves"];
        if (!string.IsNullOrWhiteSpace(diretorioChaves))
            protecao.PersistKeysToFileSystem(Directory.CreateDirectory(diretorioChaves));

        servicos.AddIdentity<Usuario, IdentityRole<Guid>>(opcoes =>
        {
            opcoes.User.RequireUniqueEmail = true;
            opcoes.Password.RequiredLength = 12;
            opcoes.Lockout.AllowedForNewUsers = true;
            opcoes.Lockout.MaxFailedAccessAttempts = 5;
            opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<FinanceiroDb>().AddDefaultTokenProviders();

        servicos.Configure<SecurityStampValidatorOptions>(opcoes => opcoes.ValidationInterval = TimeSpan.Zero);

        servicos.ConfigureApplicationCookie(opcoes =>
        {
            opcoes.Cookie.Name = "euconomia.sessao";
            opcoes.Cookie.HttpOnly = true;
            opcoes.Cookie.SameSite = SameSiteMode.Lax;
            opcoes.Cookie.SecurePolicy = politicaCookie;
            opcoes.Events.OnRedirectToLogin = contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            opcoes.Events.OnRedirectToAccessDenied = contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        servicos.AddAuthorization();
        servicos.AddAntiforgery(opcoes =>
        {
            opcoes.HeaderName = "X-XSRF-TOKEN";
            opcoes.Cookie.Name = "euconomia.antiforgery";
            opcoes.Cookie.SameSite = SameSiteMode.Strict;
            opcoes.Cookie.SecurePolicy = politicaCookie;
        });

        return servicos;
    }
}
