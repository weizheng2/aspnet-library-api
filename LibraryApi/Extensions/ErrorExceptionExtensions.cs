using Microsoft.AspNetCore.Diagnostics;
using LibraryApi.Data; 
using LibraryApi.Models; 

public static class ErrorExceptionExtensions
{
    public static IApplicationBuilder UseExceptionLogMiddleware(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error!;

                // Save error in DB
                var error = new Error
                {
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Date = DateTime.UtcNow
                };

                var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                dbContext.Errors.Add(error);
                await dbContext.SaveChangesAsync();

                // Send info to client
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                await Results.Json(new
                {
                    type = "Error",
                    message = "An error occurred",
                    status = 500
                }).ExecuteAsync(context);
            });
        });

        return app;
    }
}