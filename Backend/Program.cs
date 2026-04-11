using System.Text;
using Backend.Data;
using Backend.Models;
using Backend.Repository;
using Backend.Repository.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 33))
    )
);

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductTypeRepository, ProductTypeRepository>();

// ── Email settings ────────────────────────────────────────────────────────────
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

// ── FTP settings ──────────────────────────────────────────────────────────────
builder.Services.Configure<FtpSettings>(
    builder.Configuration.GetSection("FtpSettings")
);

// ── JWT settings ──────────────────────────────────────────────────────────────
// Override with JWT_SECRET environment variable in production.
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? jwtSection["Secret"]
    ?? throw new InvalidOperationException("JWT secret is not configured. Set the JWT_SECRET environment variable.");

builder.Services.Configure<JwtSettings>(options =>
{
    jwtSection.Bind(options);
    options.Secret = jwtSecret;
});

// ── Authentication (JWT Bearer) ───────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Controllers + JSON ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "https://arrowinstruments.in",
                "https://www.arrowinstruments.in",
                "https://arrowinstruments-production.up.railway.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════════════════════════════════════════

// Swagger enabled in all environments temporarily so endpoints can be tested
// on Railway. Remove the unconditional block and restore IsDevelopment() guard
// once the production Angular frontend is confirmed working.
app.UseSwagger();
app.UseSwaggerUI();

// NOTE: UseHttpsRedirection is intentionally removed.
// Railway (and most reverse-proxy hosts) terminate TLS externally.
// The container receives plain HTTP on port 8080. Adding HTTPS redirection
// here would cause every request to issue a 307 redirect to an HTTPS port
// that does not exist inside the container, resulting in universal 404/ERR.

app.UseRouting();

// CORS must come after routing but before authentication/authorization so that
// OPTIONS preflight requests are answered before hitting any auth check.
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
