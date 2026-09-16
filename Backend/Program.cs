using System.Text;
using System.Threading.RateLimiting;
using Backend.Data;
using Backend.Models;
using Backend.Repository;
using Backend.Repository.Interfaces;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// QuestPDF (ledger statement PDFs) — Community license is free for
// organizations with < $1M annual revenue, which covers this app.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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

// ── Reminders (email reminders + recurring schedules) ──────────────────────────
builder.Services.AddScoped<ReminderService>();
builder.Services.AddHostedService<ReminderSchedulerService>();

// ── Ledger statement export (Excel/PDF) ─────────────────────────────────────────
builder.Services.AddScoped<LedgerDocumentService>();

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

// ── Rate limiting ─────────────────────────────────────────────────────────────
// Per-client-IP limits on the two endpoints anyone on the internet can hit
// without auth: admin login (brute-force protection) and the public contact
// form (stops it being scripted into a spam/email relay).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
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

// Railway terminates TLS at its edge and proxies plain HTTP to the container,
// so the real client IP arrives via X-Forwarded-For — needed for the rate
// limiter below to key on the actual caller instead of Railway's proxy IP.
// KnownNetworks/KnownProxies are cleared because Railway's edge IP isn't a
// fixed, pinnable address; this is the standard pattern for platforms like
// Railway/Heroku that sit in front of the app as a trusted edge proxy.
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaderOptions.KnownNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

// Global exception handler — logs every unhandled exception from any current or
// future controller/middleware (with method + path) to the app's logger, which
// Railway captures from stdout, then returns a clean JSON 500 instead of the
// default HTML error page. This is the app-wide safety net; specific endpoints
// (e.g. reminder email sending) additionally log richer, stage-specific detail
// of their own before their exceptions reach here.
var isProductionForErrors = app.Environment.IsProduction();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = isProductionForErrors ? "An unexpected error occurred." : exception?.Message
        });
    });
});

// Swagger only outside Production — the production frontend is confirmed
// working, so there's no more need to expose the full API schema publicly.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

// Baseline security response headers on every response. CSP is production-only
// since it would otherwise block Swagger UI's own inline scripts/styles in
// Development — this API returns JSON everywhere else, so 'none' is safe.
var isProductionEnv = app.Environment.IsProduction();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (isProductionEnv)
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    }
    await next();
});

// NOTE: UseHttpsRedirection is intentionally removed.
// Railway (and most reverse-proxy hosts) terminate TLS externally.
// The container receives plain HTTP on port 8080. Adding HTTPS redirection
// here would cause every request to issue a 307 redirect to an HTTPS port
// that does not exist inside the container, resulting in universal 404/ERR.

app.UseRouting();

// CORS must come after routing but before authentication/authorization so that
// OPTIONS preflight requests are answered before hitting any auth check.
app.UseCors("AllowAngularApp");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
