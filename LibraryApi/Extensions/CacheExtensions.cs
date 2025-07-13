namespace LibraryApi.Extensions
{
    public static class CacheExtensions
    {
        public static IServiceCollection AddCustomCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOutputCache(options =>
            {
                options.AddBasePolicy(policy =>
                {
                    policy.Expire(TimeSpan.FromMinutes(5));
                });
            });

            services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
            });

            return services;
        }
    }
}