using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Identity;
using QuranCompanion.Infrastructure.Persistence;
using QuranCompanion.Infrastructure.Services;

namespace QuranCompanion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Database ---
        // Provider is chosen via configuration ("Database:Provider": "Postgres" | "SqlServer")
        // so the team can pick per spec section 21 without code changes.
        var provider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        // --- Identity ---
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // flip to true once email delivery is wired up in production
        })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // --- JWT ---
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        // --- Google Sign-In ---
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // --- App services ---
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IWirdIdGenerator, WirdIdGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IQuranService, QuranService>();
        services.AddScoped<IReadingProgressService, ReadingProgressService>();
        services.AddScoped<IWirdService, WirdService>();
        services.AddScoped<IBookmarkService, BookmarkService>();

        // Tafsir: HttpClient-backed provider with a short timeout so a slow/unreachable
        // external source degrades gracefully (see TafsirService's try/catch) instead of
        // hanging the request.
        services.AddHttpClient<ITafsirProvider, QuranTafseerComProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
        });
        services.AddScoped<ITafsirService, TafsirService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICompanionService, CompanionService>();
        services.AddScoped<IPrivacyService, PrivacyService>();
        services.AddScoped<IEncouragementService, EncouragementService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        // --- Email ---
        // Uses real SMTP (e.g. Gmail) once Smtp:* is filled in in configuration;
        // otherwise falls back to logging the message/code to the console for local dev.
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        var smtpSettings = configuration.GetSection(SmtpSettings.SectionName).Get<SmtpSettings>() ?? new SmtpSettings();
        if (smtpSettings.IsConfigured)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        return services;
    }
}
