using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WarehouseApi.Data;
using WarehouseApi.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 1. ADD SIGNALR SERVICE ---
builder.Services.AddSignalR(); 

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("GlobalSyntaxPolicy", policy =>
    {
        policy.WithOrigins(
                "https://gsyntaxhosting.com", 
                "https://gsyntaxhserver.com", 
                "http://gsyntaxserver.com", 
                "http://gsyntaxhosting.com", 
                "http://localhost:4200", 
                "https://freshv-gnf6c8cfhxbdc9gt.westus2-01.azurewebsites.net"
              ) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              // --- 2. CRITICAL FOR SIGNALR ---
              // You MUST use AllowCredentials() and you CANNOT use AllowAnyOrigin (*)
              .AllowCredentials() 
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

var app = builder.Build();

// --- 3. MIDDLEWARE ORDER MATTERS ---
app.UseRouting();

// WebSockets must be enabled before the Hub mapping
app.UseWebSockets(); 

app.UseCors("GlobalSyntaxPolicy");

// Cache-Control Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseAuthentication(); // Ensure this is here if using JWT
app.UseAuthorization();

app.MapControllers();

// --- 4. MAP THE HUB ---
app.MapHub<PriceHub>("/pricehub");

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Warehouse API v1");
    });
}

app.UseHttpsRedirection();

// ... WeatherForecast MapGet logic ...

app.Run();