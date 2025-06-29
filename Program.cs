using Microsoft.EntityFrameworkCore;
using UdemyBibliotecaApi.Data;
using UdemyBibliotecaApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Start Services

//builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddControllers().AddNewtonsoftJson();
//builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Start Middlewares

app.UseLogPetition();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapControllers();

app.Run();
