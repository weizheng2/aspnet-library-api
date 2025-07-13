namespace LibraryApi.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddAllowedHostsCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedHosts = configuration.GetSection("allowedHosts").Get<string[]>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedHosts!)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .WithExposedHeaders("total-records-quantity");
                });
            });

            return services;
        }
    }
}