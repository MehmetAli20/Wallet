using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;
using Wallet.Api.Authentication;
using Wallet.Api.Configuration;
using Wallet.Api.Middleware;
using Wallet.Api.RateLimiting;
using Wallet.Application;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Users;
using Wallet.Infrastructure;
using Wallet.Infrastructure.Authentication;
using Wallet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var rateLimiting = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

rateLimiting.EnsureUsable();

if (rateLimiting.Enabled && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Redis")))
{
    throw new InvalidOperationException(
        "RateLimiting is enabled but ConnectionStrings:Redis is not set. " +
        "Rate limiting needs Redis to share counters across instances. " +
        "Set the connection string, or set RateLimiting:Enabled to false.");
}

builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));

builder.Services.AddControllers(options =>
{
    if (rateLimiting.Enabled)
    {
        options.Filters.Add<RateLimitFilter>();
    }
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the token returned by /api/v1/auth/login."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<WalletDbContext>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

var forwarding = builder.Configuration
    .GetSection(ForwardingOptions.SectionName)
    .Get<ForwardingOptions>() ?? new ForwardingOptions();

builder.Services.Configure<ForwardedHeadersOptions>(forwarding.ApplyTo);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt section is not configured.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
    || System.Text.Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be at least 32 bytes. Set it in configuration, user-secrets, or the JWT_SIGNING_KEY environment variable.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});


var app = builder.Build();

if (forwarding.Enabled)
{
    app.UseForwardedHeaders();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Wallet API v1");
        options.RoutePrefix = "swagger";
    });

    var seedUsername = builder.Configuration["Seed:AdminUsername"];
    var seedEmail = builder.Configuration["Seed:AdminEmail"];
    var seedPassword = builder.Configuration["Seed:AdminPassword"];

    var seedValues = new[] { seedUsername, seedEmail, seedPassword };

    if (seedValues.All(string.IsNullOrWhiteSpace))
    {
        app.Logger.LogInformation("No admin seed configuration found, skipping seed.");
    }
    else if (seedValues.Any(string.IsNullOrWhiteSpace))
    {
        throw new InvalidOperationException(
            "Admin seed is partially configured. Seed:AdminUsername, Seed:AdminEmail and Seed:AdminPassword must all be set, or none of them.");
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<AdminUserSeeder>();
        await seeder.SeedAsync(seedUsername!, seedEmail!, seedPassword!);
    }
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }
