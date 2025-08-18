
namespace LibraryApi.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddAuthorizationBasedOnPolicy(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.PolicyIsAdmin, policy => policy.RequireClaim(Constants.PolicyIsAdmin));
            });

            return services;
        }
    }
}