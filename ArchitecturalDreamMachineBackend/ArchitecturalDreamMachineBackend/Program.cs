using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Export;
using ArchitecturalDreamMachineBackend.Services;
using ArchitecturalDreamMachineBackend.Middleware;
using ArchitecturalDreamMachineBackend.Validators;

var builder = WebApplication.CreateBuilder(args);

// Configure max request body size (4 KB is sufficient for this API)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 4096;
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<GenerateRequestValidator>();

// Register geometry and orchestration services
builder.Services.AddScoped<IGeometryService, GeometryService>();
builder.Services.AddScoped<ILayoutService, LayoutService>();
builder.Services.AddScoped<IRoofService, RoofService>();
builder.Services.AddScoped<IWindowService, WindowService>();
builder.Services.AddScoped<IInteriorWallService, InteriorWallService>();
builder.Services.AddScoped<IDesignOrchestrationService, DesignOrchestrationService>();
builder.Services.AddScoped<IHouseParametersService, HouseParametersService>();
builder.Services.AddScoped<IIfcExporter, IfcExporter>();
builder.Services.AddScoped<IGltfExporter, GltfExporter>();

// Configure CORS for React Native frontend
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure DbContext with SQLite (fallback to SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("DatabaseConfig");
    var sqlServerConnection = Environment.GetEnvironmentVariable("SQL_SERVER_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(sqlServerConnection))
    {
        try
        {
            options.UseSqlServer(sqlServerConnection);
            logger.LogInformation("Using SQL Server database connection");
        }
        catch (Exception ex)
        {
            if (builder.Environment.IsDevelopment())
            {
                logger.LogWarning("Failed to configure SQL Server, falling back to SQLite");
                logger.LogDebug(ex, "SQL Server configuration error details");
                options.UseSqlite("Data Source=architecturaldreammachine.db");
            }
            else
            {
                throw new InvalidOperationException(
                    "Failed to configure SQL Server database. SQLite fallback is not allowed in production.", ex);
            }
        }
    }
    else
    {
        options.UseSqlite("Data Source=architecturaldreammachine.db");
    }
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseRateLimiter();
app.UseCors();
app.MapControllers();

app.Run();
