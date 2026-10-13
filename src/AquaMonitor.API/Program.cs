using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AquaMonitor.API.Data;
using AquaMonitor.API.Health;
using AquaMonitor.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
    });

// ─── Banco de Dados Oracle ────────────────────────────────────────────────────
// A connection string NÃO fica mais fixa no código: em Docker/CI ela chega pela
// variável de ambiente ConnectionStrings__OracleConnection (12-factor app).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

// ─── Health Check (usado pelo Docker, pelo docker-compose e pelo pipeline) ────
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("oracle-db");

// ─── JWT Auth ─────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ─── Serviços ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();

// ─── Swagger ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AquaMonitor API",
        Version = "v1",
        Description = "Sistema de monitoramento inteligente do consumo de água - ESG Tema 7"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AquaMonitor API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── /health: status da API + banco, ambiente e versão publicada ─────────────
// Permite comprovar nos prints qual ambiente (Staging/Production) e qual versão
// (commit SHA) estão rodando.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            ambiente = app.Environment.EnvironmentName,
            versao = app.Configuration["APP_VERSION"] ?? "dev",
            timestampUtc = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                descricao = e.Value.Description
            })
        });
    }
});

// ─── Migrations automáticas ao iniciar (com retry) ───────────────────────────
// Em container, a API pode subir antes do Oracle aceitar conexões. Por isso
// tentamos aplicar as migrations algumas vezes antes de desistir.
// Nos testes o provider é InMemory (não relacional), então Migrate() é ignorado.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (db.Database.IsRelational())
    {
        var maxTentativas = app.Configuration.GetValue("Database:MigrationRetries", 12);
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Migrations aplicadas com sucesso (tentativa {Tentativa}).", tentativa);
                break;
            }
            catch (Exception ex) when (tentativa < maxTentativas)
            {
                logger.LogWarning("Banco indisponível (tentativa {Tentativa}/{Max}): {Erro}. Nova tentativa em 5s...",
                    tentativa, maxTentativas, ex.Message);
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }
}

app.Run();

// Necessário para testes de integração
public partial class Program { }
