using Microsoft.EntityFrameworkCore;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register geometry and orchestration services
builder.Services.AddScoped<GeometryService>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<RoofService>();
builder.Services.AddScoped<DesignOrchestrationService>();

// Configure CORS for React Native frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure DbContext with SQLite (fallback to SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var sqlServerConnection = Environment.GetEnvironmentVariable("SQL_SERVER_CONNECTION_STRING");
    if (!string.IsNullOrEmpty(sqlServerConnection))
    {
        options.UseSqlServer(sqlServerConnection);
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
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
