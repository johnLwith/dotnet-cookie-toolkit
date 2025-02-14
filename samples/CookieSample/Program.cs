using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddDataProtection()
    .SetApplicationName(builder.Configuration.GetValue<string>("DataProtection:ApplicationName") ?? "Dotnet.DeCookie");

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
