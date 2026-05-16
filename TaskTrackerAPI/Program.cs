using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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

var config = builder.Configuration;

builder.Services.AddDbContext<TaskDbContext>(opt => opt.UseSqlServer(config.GetConnectionString("taskDbConn")));

builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();