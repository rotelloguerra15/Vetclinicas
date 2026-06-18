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

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

// ── Banco de dados ────────────────────────────────────────────────────────────
// PlatformDbContext: schema "platform" — super-admin + lista de clínicas
builder.Services.AddDbContext<PlatformDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// TenantDbContextFactory: instanciado por request com o schema do tenant logado
builder.Services.AddScoped<TenantDbContextFactory>();

// ── Serviços ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProvisionamentoService>();
builder.Services.AddScoped<AgendaService>();
builder.Services.AddScoped<AgendamentoLinkService>();
builder.Services.AddHttpClient<WhatsAppService>();
builder.Services.AddHostedService<NotificacaoDispatcher>();
builder.Services.AddHostedService<AgendadorMensagens>();
builder.Services.AddHttpClient<ReceituarioPdfService>();
builder.Services.AddScoped<BotWAService>();
builder.Services.AddScoped<FuncionarioService>();
builder.Services.AddScoped<ComissaoService>();
builder.Services.AddScoped<FechamentoService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<VetClinica.API.Services.Payments.IPaymentProvider, VetClinica.API.Services.Payments.AsaasPaymentProvider>();
builder.Services.AddScoped<VetClinica.API.Services.Payments.MercadoPagoProvider>();

// ── JWT ───────────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? throw new InvalidOperationException("Jwt:Key nao configurado. Adicione a variavel Jwt__Key.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opt =>
    opt.AddPolicy("front", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-WhatsApp-Enviado", "Content-Disposition")));

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
        In = ParameterLocation.Header, Description = "JWT: Bearer {token}",
        Name = "Authorization", Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{
        new OpenApiSecurityScheme
            { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// ── Garante schema "platform" na primeira inicialização ───────────────────────
using (var scope = app.Services.CreateScope())
{
    var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await platformDb.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("front");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", ts = DateTime.UtcNow }));

app.Run();
