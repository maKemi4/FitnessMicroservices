using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// CORS Політика
builder.Services.AddCors(options =>
{
    options.AddPolicy("DepartmentSite", policy =>
    {
        policy
            .WithOrigins("https://cad.kpi.ua", "http://cad.kpi.ua")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type");
    });
});

IConfiguration proxyConfiguration;

if (builder.Environment.IsProduction())
{
    proxyConfiguration = new ConfigurationBuilder()
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile(
            "appsettings.Docker.json",
            optional: false,
            reloadOnChange: true)
        .Build();
}
else
{
    proxyConfiguration = builder.Configuration;
}

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(
        proxyConfiguration.GetSection("ReverseProxy"));

const string issuer = "http://localhost:8081/realms/fitness";

// Перевірку JWT-токенів
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.Audience = "fitness-api";
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
            ?? $"{issuer}/.well-known/openid-configuration";

        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    });

// Налаштування прав доступу до API
builder.Services.AddAuthorization(options =>
{
    string[] scopes =
    [
        "fitness.users",
        "fitness.load",
        "fitness.trainings",
        "fitness.statistics"
    ];

    foreach (var scope in scopes)
    {
        options.AddPolicy(scope, policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireAssertion(context =>
                context.User.HasClaim(claim =>
                    claim.Type == "scope" &&
                    claim.Value
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Contains(scope, StringComparer.Ordinal)));
        });
    }
});

var app = builder.Build();

// Middleware
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "Fitness API Gateway",
        Status = "Running"
    }));

app.MapReverseProxy();

app.Run();
