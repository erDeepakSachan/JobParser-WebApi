using JobParser.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
{
    var logFile = context.Configuration["Logging:FilePath"] ?? "Logs/job-parser-.log";

    config
        .MinimumLevel.Information()
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.File(
            path: logFile,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true);
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapPost("/", () => new
{
    service = "JobParser API",
    status = "Running",
    swagger = "/swagger",
    health = "/health",
    webhook = "/api/telegram/webhook"
});

app.Run();