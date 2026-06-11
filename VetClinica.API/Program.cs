using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VetClinica.API.Data;
using VetClinica.API.Middleware;
using VetClinica.API.Services;
using VetClinica.API.Services.RH;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// Railway injeta a porta via variavel PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

// ---------- Banco ----------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---------- Multi-tenant (escopo por request) ----------
builder.Services.AddScoped<TenantContext>();

// ---------- Serviços ----------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProvisionamentoService>();
builder.Services.AddScoped<AgendaService>();
builder.Services.AddScoped<AgendamentoLinkService>();
builder.Services.AddHttpClient<ZApiService>();
builder.Services.AddHostedService<NotificacaoDispatcher>();
builder.Services.AddHostedService<AgendadorMensagens>();

// PDF / Receituario
builder.Services.AddHttpClient<ReceituarioPdfService>();

// Bot WhatsApp
builder.Services.AddScoped<BotWhatsAppService>();

// RH
builder.Services.AddScoped<FuncionarioService>();
builder.Services.AddScoped<ComissaoService>();
builder.Services.AddScoped<FechamentoService>();

// IA Diagnostico — HttpClient generico para IaController
builder.Services.AddHttpClient();

// ---------- JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? throw new InvalidOperationException("Jwt:Key nao configurado. Adicione a variavel Jwt__Key no Railway.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
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

// ---------- CORS ----------
builder.Services.AddCors(opt =>
    opt.AddPolicy("front", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-WhatsApp-Enviado", "Content-Disposition", "X-WhatsApp-Enviado")));

// ---------- Controllers + Swagger ----------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VetClinica API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("front");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();   // popula TenantContext a partir do JWT
app.UseAuthorization();
app.MapControllers();

// Health check para Railway
app.MapGet("/health", () => Results.Ok(new { status = "healthy", ts = DateTime.UtcNow }));

app.Run();
 
 
