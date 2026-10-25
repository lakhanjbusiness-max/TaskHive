using Microsoft.EntityFrameworkCore;
using TaskHive.Api.Auth;
using TaskHive.Application.Common;
using TaskHive.Infrastructure.Persistence;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using TaskHive.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services: the DI container ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken from /api/auth/login. Swagger adds the 'Bearer ' prefix."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Makes the current request reachable from non-endpoint classes. Singleton.
builder.Services.AddHttpContextAccessor();

// Scoped: one per HTTP request, matching the DbContext's lifetime.
builder.Services.AddScoped<ITenantContext, CurrentTenantContext>();

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<ITokenService, JwtTokenService>();

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Jwt settings are missing. Did you set user secrets on TaskHive.Api?");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// AddDbContext registers AppDbContext as Scoped by default.
//AppDbContext must be Scoped. It holds a change tracker; sharing one across concurrent requests would mix different users'
//pending changes — and in a multi-tenant app that's a data-leak bug, not just a race.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

//ITenantContext is Scoped to match. Here's the rule that gets asked about: a longer-lived service must never capture a shorter-lived one.
//If you registered AppDbContext as a Singleton while ITenantContext stayed Scoped, the singleton would capture one request's tenant context and hold it forever — a captive dependency.
//ASP.NET Core validates this in the Development environment and throws at startup rather than letting you ship the bug.

var app = builder.Build();

// ---------- Middleware pipeline: ORDER MATTERS ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();