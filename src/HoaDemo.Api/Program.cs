using System.Text.Json.Serialization;
using HoaDemo.Api.Data;
using HoaDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("HoaContext");

if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<HoaContext, SqliteHoaContext>(o => o.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<HoaContext>(o => o.UseSqlServer(connectionString));
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILateFeeCalculator, LateFeeCalculator>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        // Web API 2 compatibility: NotFound()/BadRequest() had no ProblemDetails body.
        o.SuppressMapClientErrors = true;
        o.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new
        {
            message = "The request is invalid.",
            modelState = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).ToArray())
        });
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await HoaDbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }
