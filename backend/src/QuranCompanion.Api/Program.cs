using QuranCompanion.Api.Middleware;
using QuranCompanion.Infrastructure;
using QuranCompanion.Infrastructure.Persistence;
using QuranCompanion.Infrastructure.QuranImport;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// `dotnet run -- import-quran <path-to-tanzil-file>` imports the full Quran
// text and exits, without starting the web server. Run it after the app has
// started at least once in Development (so Surahs metadata is seeded).
//
// `dotnet run -- update-pages-juz <path-to-pages-juz-json>` writes the verified
// Mushaf 604-page / 30-juz numbers onto the imported ayahs and exits. Requires
// import-quran to have run first.
if (args.Length >= 2 && (args[0] == "import-quran" || args[0] == "update-pages-juz"))
{
    builder.Services.AddInfrastructure(builder.Configuration);
    using var importHost = builder.Build();
    using var scope = importHost.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (args[0] == "import-quran")
    {
        var result = await TanzilImporter.ImportAsync(db, args[1]);
        Console.WriteLine($"Quran import complete. Inserted: {result.Inserted}, already present: {result.Skipped}, lines read: {result.TotalLinesRead}.");
    }
    else
    {
        var result = await TanzilImporter.ImportPageJuzAsync(db, args[1]);
        Console.WriteLine($"Pages/juz import complete. Rows: {result.RowsRead}, already correct: {result.AlreadyCorrect}, updated: {result.Updated}.");
    }
    return;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the access token (without 'Bearer ' prefix)."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required so the httpOnly refresh-token cookie is sent/received
    });
});

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


// Apply pending EF Core migrations and seed reference data (Surahs, etc.) on
// every startup, in every environment - including production. MonsterASP
// (and most shared hosts) don't give a console to run `dotnet ef database
// update` against the live app process, so the app has to create/update its
// own schema on boot instead of relying on a Development-only code path.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await QuranSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
