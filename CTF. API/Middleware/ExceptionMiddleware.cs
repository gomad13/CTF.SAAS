namespace CTF.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var message = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            // Dev-only : on retourne aussi le stack pour faciliter le debug.
            // Comportement prod inchangé (message générique).
            if (_env.IsDevelopment())
                await context.Response.WriteAsJsonAsync(new { error = message, type = ex.GetType().FullName, stack = ex.StackTrace?.Split('\n') });
            else
                await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}
