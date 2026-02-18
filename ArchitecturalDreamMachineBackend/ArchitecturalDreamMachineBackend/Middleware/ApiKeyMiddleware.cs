namespace ArchitecturalDreamMachineBackend.Middleware;

/// <summary>
/// Middleware that validates API key from the X-Api-Key header.
/// In development mode, authentication is skipped if no API key is configured.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _next = next;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var configuredApiKey = _configuration["ApiKey"]
            ?? Environment.GetEnvironmentVariable("API_KEY");

        // In development, skip auth if no API key is configured
        if (string.IsNullOrEmpty(configuredApiKey))
        {
            if (_environment.IsDevelopment())
            {
                await _next(context);
                return;
            }

            // In production, require an API key to be configured
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Server misconfiguration" });
            return;
        }

        // Allow Swagger UI in development without auth
        if (_environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey)
            || !string.Equals(configuredApiKey, providedApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await _next(context);
    }
}
