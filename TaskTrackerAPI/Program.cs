using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using TaskTrackerAPI.Models;
using TaskTrackerAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Task Tracker API",
        Description = "An ASP.NET Core Web API for managing tasks in a task tracking application."
    });
});

//Add CORS
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

builder.Services.AddScoped<ITaskService, TaskService>();


builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter(opt =>
    {
        opt.Endpoint = new Uri("http://otel-collector:4317");
        opt.Protocol = OtlpExportProtocol.Grpc;
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
    .AddService("TaskTrackerAPI")
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = builder.Environment.EnvironmentName,
        ["service.version"] = "1.0.0"
    })
    )


    .WithTracing(tracing => tracing
        .AddSource("TaskTrackerAPI")
        .AddAspNetCoreInstrumentation()
        .AddSqlClientInstrumentation(options =>
        {
            options.EnrichWithSqlCommand = (activity, command) =>
            {
                if (command is SqlCommand cmd)
                {
                    // Capture actual SQL queries (equivalent to SetDbStatementForText = true)
                    activity.SetTag("db.statement", cmd.CommandText);
                    activity.SetTag("db.commandTimeOut", cmd.CommandTimeout);
                }
            };
            options.RecordException = true;
        })
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        }))

    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .SetExemplarFilter(ExemplarFilterType.TraceBased) // This is the key line
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }));


;

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection(); Removed HttpsRedirection as this will be on private subnet and never exposed
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();