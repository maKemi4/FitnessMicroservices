using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "Fitness API Gateway",
        Status = "Running"
    }));

app.MapReverseProxy();

app.Run();
