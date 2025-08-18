using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LibraryApi.Swagger
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

                options.OperationFilter<AuthorizationFilter>();
            });

            return services;
        }

        public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();

                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        var endpoint = $"/swagger/{description.GroupName}/swagger.json";
                        var name = $"Library API {description.GroupName.ToUpperInvariant()}";

                        //Console.WriteLine($"Adding Swagger endpoint: {endpoint} with name: {name}");

                        options.SwaggerEndpoint(endpoint, name);
                    }
                });
            }

            return app;
        }

        public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
        {
            private readonly IApiVersionDescriptionProvider _provider;

            public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
            {
                _provider = provider;
            }

            public void Configure(SwaggerGenOptions options)
            {
                foreach (var description in _provider.ApiVersionDescriptions)
                {
                    var apiVersion = description.ApiVersion.ToString();
                    var groupName = description.GroupName;

                    string customDescription = "Web API to work with authors and book data";

                    if (description.IsDeprecated)
                    {
                        customDescription = $"Version {apiVersion} is deprecated. Please migrate to a newer version.";
                    }
                    else
                    {
                        /*
                        customDescription = groupName switch
                        {
                            "v1" => "Web API to work with authors and book data.<br/>" +
                                    "This version has authorization enabled. Try V2 for a public version.<br/>",
                            "v2" => "Web API to work with authors and book data.<br/>This version does not have authorization enabled. Try V1 for a version with authorization.",
                            _ => "Web API to work with authors and book data"
                        };
                        */
                     }
                 
                    options.SwaggerDoc(groupName, new OpenApiInfo
                    {
                        Title = "Library API",
                        Version = apiVersion,
                        Description = customDescription,
                        Contact = new OpenApiContact
                        {
                            Name = "Wei",
                            Url = new Uri("https://github.com/weizheng2")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT",
                            Url = new Uri("https://opensource.org/license/mit/")
                        }
                    });
                }
            }
        }
    }
}