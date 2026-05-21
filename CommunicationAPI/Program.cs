using CommunicationAPI;
using CommunicationAPI.Models;

using Confluent.Kafka;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Background Monitor API",
        Description = "An ASP.NET Core Web API for monitoring background tasks."
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var config = builder.Configuration;
builder.Services.AddDbContext<TaskDbContext>(opt => opt.UseSqlServer(config.GetConnectionString("taskDbConn")));


// ==========================================
// 1. OPEN TELEMETRY LOGGING CONFIGURATION
// ==========================================
builder.Logging.ClearProviders(); // Optional: Clears console if you want only OTLP logs
builder.Logging.AddConsole();    // Optional: Keeps local container console streaming
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true; // CRITICAL: Allows Loki to see structured attributes like TraceId/SpanId

    logging.AddOtlpExporter(opt =>
    {
        opt.Endpoint = new Uri("http://otel-collector:4317");
        opt.Protocol = OtlpExportProtocol.Grpc;
    });
});

// ==========================================
// 2. OPEN TELEMETRY TRACES & METRICS CONFIGURATION
// ==========================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("BackgroundMonitorAPI")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.version"] = "1.0.0"
        })
    )
    .WithTracing(tracing => tracing
        .AddSource("BackgroundMonitorAPI")
        .AddAspNetCoreInstrumentation()
        .AddSqlClientInstrumentation(options =>
        {
            options.EnrichWithSqlCommand = (activity, command) =>
            {
                if (command is SqlCommand cmd)
                {
                    activity.SetTag("db.statement", cmd.CommandText);
                    activity.SetTag("db.commandTimeOut", cmd.CommandTimeout);
                }
            };
            options.RecordException = true;
        })
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .SetExemplarFilter(ExemplarFilterType.TraceBased) // Key line for Grafana Diamond Indicators
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }));


// Kafka Consumer
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        GroupId = "task-service-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    return new ConsumerBuilder<string, string>(config).Build();
});

builder.Services.AddHostedService<BackgroundMonitor>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
