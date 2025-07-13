using LibraryApi.Data;
using LibraryApi.Models;

namespace LibraryApi.Middlewares
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                dbContext.Errors.Add(new Error
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Date = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync();

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var result = Results.Json(new
                {
                    type = "Error",
                    message = "An error occurred",
                    status = 500
                });

                await result.ExecuteAsync(context);
            }
        }
    }

    public static class ExceptionLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionLoggingMiddleware>();
        }
    }
}