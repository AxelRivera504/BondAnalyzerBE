using BondAnalyzer.API.Data;
using BondAnalyzer.API.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF licencia community (libre para proyectos académicos)
QuestPDF.Settings.License = LicenseType.Community;

// Controllers + JSON camelCase
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Bond Analyzer API",
        Version = "v1",
        Description = "API para análisis de rentabilidad y riesgo de bonos — Smart Finance | UNAH IS-820"
    });
});

// Entity Framework Core — SQL Server
builder.Services.AddDbContext<BondDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicios de negocio
builder.Services.AddScoped<IBondCalculatorService, BondCalculatorService>();
builder.Services.AddScoped<ExportService>();

// CORS — orígenes permitidos leídos desde appsettings (local + producción)
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Aplicar migraciones pendientes automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BondDbContext>();
    db.Database.Migrate();
}

// Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bond Analyzer API v1");
        c.RoutePrefix = "swagger";
    });
}

// NO usar UseHttpsRedirection en producción:
// Somee maneja HTTPS a nivel de su proxy/IIS; redirigir aquí causaría bucles.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ReactFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
