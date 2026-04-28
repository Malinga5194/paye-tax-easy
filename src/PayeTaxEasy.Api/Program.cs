using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;
using PayeTaxEasy.Infrastructure;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DB, Services) ─────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "PAYE Tax Easy API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer dev-employer  (or dev-employee / dev-ird)"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

// ── DEV Authentication — symmetric JWT with a local secret ───────────────────
// In production this would be replaced with Azure AD B2C (RS256).
// Dev tokens are signed with a local secret and validated here.
var devSecret = "paye-tax-easy-dev-secret-key-32chars!!";
var devKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(devSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "paye-tax-easy-dev",
            ValidateAudience = true,
            ValidAudience = "paye-tax-easy-api",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = devKey,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// ── CORS — allow all Vite dev ports ──────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontends", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "http://localhost:5176",
                "http://localhost:5177",
                "http://localhost:5178",
                "http://localhost:5179"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Always show Swagger in dev
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PAYE Tax Easy API v1"));

app.UseCors("LocalFrontends");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// ── DEV: /auth/login endpoint — issues local JWT tokens ──────────────────────
app.MapPost("/auth/login", async (LoginRequest req, PayeTaxEasy.Infrastructure.Data.PayeTaxEasyDbContext db) =>
{
    // Check database users first
    var user = await db.AppUsers.FirstOrDefaultAsync(u =>
        u.Email.ToLower() == req.Email.ToLower() && u.IsActive);

    string role, tin, name;

    if (user != null)
    {
        // Verify password hash
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Results.Json(new { errorCode = "AUTH_001", message = "Invalid credentials." }, statusCode: 401);
        role = user.Role;
        tin = user.TIN;
        name = user.FullName;
    }
    else
    {
        // Fall back to hardcoded dev users
        var devUsers = new Dictionary<string, (string role, string tin, string name)>
        {
            ["employer@test.com"]  = ("Employer",    "EMP001TIN",  "ABC Company Ltd"),
            ["employee@test.com"]  = ("Employee",    "EMP123456V", "John Silva"),
            ["ird@test.com"]       = ("IRD_Officer", "IRD001",     "IRD Officer"),
            ["admin@test.com"]     = ("SystemAdmin", "ADM001",     "System Admin"),
        };
        if (!devUsers.TryGetValue(req.Email.ToLower(), out var devUser) || req.Password != "Test@1234")
            return Results.Json(new { errorCode = "AUTH_001", message = "Invalid credentials." }, statusCode: 401);
        role = devUser.role; tin = devUser.tin; name = devUser.name;
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user?.Id.ToString() ?? Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Email, req.Email),
        new Claim(ClaimTypes.Role, role),
        new Claim("extension_TIN", tin),
        new Claim("name", name),
        new Claim("sub", user?.Id.ToString() ?? Guid.NewGuid().ToString()),
    };

    var creds = new SigningCredentials(devKey, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "paye-tax-easy-dev",
        audience: "paye-tax-easy-api",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { accessToken = tokenStr, role, name, expiresIn = 28800 });
}).AllowAnonymous();

// ── /auth/register endpoint ───────────────────────────────────────────────────
app.MapPost("/auth/register", async (RegisterRequest req, PayeTaxEasy.Infrastructure.Data.PayeTaxEasyDbContext db) =>
{
    // Validate
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password)
        || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Role))
        return Results.Json(new { errorCode = "REG_001", message = "All fields are required." }, statusCode: 422);

    var validRoles = new[] { "Employer", "Employee", "IRD_Officer" };
    if (!validRoles.Contains(req.Role))
        return Results.Json(new { errorCode = "REG_002", message = "Invalid role. Must be Employer, Employee, or IRD_Officer." }, statusCode: 422);

    var exists = await db.AppUsers.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower());
    if (exists)
        return Results.Json(new { errorCode = "REG_003", message = "An account with this email already exists." }, statusCode: 409);

    var newUser = new PayeTaxEasy.Infrastructure.Entities.AppUser
    {
        Email = req.Email.ToLower(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
        Role = req.Role,
        FullName = req.FullName,
        TIN = req.TIN ?? string.Empty,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    db.AppUsers.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Account created successfully.", userId = newUser.Id, role = newUser.Role });
}).AllowAnonymous();

app.Run();

record LoginRequest(string Email, string Password);
record RegisterRequest(string Email, string Password, string FullName, string Role, string? TIN);
