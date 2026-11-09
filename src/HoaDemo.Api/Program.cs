using System.Text.Json;
using System.Text.Json.Serialization;
using HoaDemo.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        var json = options.JsonSerializerOptions;
        json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        json.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        json.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        json.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<HoaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HoaContext")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    HoaDbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<HoaContext>());
}

app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory<Program> in integration tests.
public partial class Program
{
}
