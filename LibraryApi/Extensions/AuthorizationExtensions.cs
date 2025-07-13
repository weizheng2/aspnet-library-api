
namespace LibraryApi.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddAuthorizationBasedOnPolicy(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("isAdmin", policy => policy.RequireClaim("isAdmin"));
            });

            return services;
        }
    }
}