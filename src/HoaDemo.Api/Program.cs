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
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
