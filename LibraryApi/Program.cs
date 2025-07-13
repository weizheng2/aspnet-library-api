using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LibraryApi.Data;
using LibraryApi.Middlewares;
using LibraryApi.Models;
using LibraryApi.OptionsConfiguration;
using LibraryApi.Services;
using LibraryApi.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Start Services
builder.Services.AddDataProtection();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy =>
    {
        policy.Expire(TimeSpan.FromMinutes(5));
    });
});

builder.Services.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Set allowed hosts with CORS
var allowedHosts = builder.Configuration.GetSection("allowedHosts").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(optionsCors =>
    {
        optionsCors.WithOrigins(allowedHosts!).AllowAnyHeader().AllowAnyMethod()
        .WithExposedHeaders("total-records-quantity");
    });
});
 
// Test get strongly typed configuration data 
builder.Services.AddOptions<PersonOptions>()
    .Bind(builder.Configuration.GetSection(PersonOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers().AddNewtonsoftJson();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Users Database
builder.Services.AddIdentityCore<User>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<SignInManager<User>>();
builder.Services.AddTransient<IUserServices, UserServices>();
builder.Services.AddTransient<IHashService, HashService>();
builder.Services.AddTransient<IArchiveStorage, ArchiveStorageAzure>();
//builder.Services.AddTransient<IArchiveStorage, ArchiveStorageLocal>();

// Represents everything about the current HTTP Request
builder.Services.AddHttpContextAccessor();

// JWT
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization based on policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("isAdmin", policy => policy.RequireClaim("isAdmin"));
});

builder.Services.AddCustomSwagger();

var app = builder.Build();

// Start Middlewares

app.UseCustomSwagger();

app.UseLogPetition();

app.UseStaticFiles();

app.UseOutputCache();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
