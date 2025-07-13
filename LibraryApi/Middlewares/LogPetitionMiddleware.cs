namespace LibraryApi.Middlewares
{
    public class LogPetitionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogPetitionMiddleware> _logger;

        public LogPetitionMiddleware(RequestDelegate next, ILogger<LogPetitionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering(); // Allows reading the body multiple times
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            _logger.LogInformation($"Petition: {context.Request.Method} {context.Request.Path}\nBody: {body}");

            // Invoke the rest of middlewares
            // With await we suspend this middleware until the next one is completed
            await _next(context);

            _logger.LogInformation($"Response: {context.Response.StatusCode}");
        }
    }

    public static class LogPetitionMiddlewareExtensions
    {
        public static IApplicationBuilder UseLogPetition(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogPetitionMiddleware>();
        }
    }
}