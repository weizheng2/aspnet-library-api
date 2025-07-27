using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Data;
using LibraryApi.Middlewares;
using LibraryApi.Models;
using LibraryApi.Services;
using LibraryApi.Swagger;
using LibraryApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Start Services
builder.Services.AddHttpContextAccessor(); // Represents everything about the current HTTP Request
builder.Services.AddDataProtection();
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddCustomRateLimiting();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationBasedOnPolicy();

builder.Services.AddAllowedHostsCors(builder.Configuration);
builder.Services.AddCustomCaching(builder.Configuration); // Redis Cache

builder.Services.AddCustomApiVersioning();
builder.Services.AddCustomSwagger();

// TEST get strongly typed configuration data 
//builder.Services.AddOptions<PersonOptions>().Bind(builder.Configuration.GetSection(PersonOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Users Database
builder.Services.AddIdentityCore<User>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<SignInManager<User>>();

builder.Services.AddTransient<IUserServices, UserServices>();
builder.Services.AddTransient<IHashService, HashService>();
builder.Services.AddTransient<IArchiveStorage, ArchiveStorageAzure>();
//builder.Services.AddTransient<IArchiveStorage, ArchiveStorageLocal>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

// Start Middlewares
app.UseCustomSwagger();
app.UseStaticFiles();
//app.UseOutputCache(); // Redis Cache
app.UseCors();
app.UseRateLimiter();

app.UseExceptionLogging();
//app.UseLogPetition();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();