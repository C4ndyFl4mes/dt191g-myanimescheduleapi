using App.Data;
using App.Services;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var env = builder.Environment;
Env.Load("../.env");

var host = Environment.GetEnvironmentVariable("MYSQLHOST")
       ?? throw new Exception("MYSQLHOST not set");
var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
var database = Environment.GetEnvironmentVariable("MYSQLDATABASE")
    ?? throw new Exception("MYSQLDATABASE not set");
var user = Environment.GetEnvironmentVariable("MYSQLUSER")
    ?? throw new Exception("MYSQLUSER not set");
var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD")
    ?? throw new Exception("MYSQLPASSWORD not set");

var connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};";

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(9, 0, 0)));
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My Anime Schedule API", Version = "v1" });
});
builder.Services.AddHostedService<AnimeIndexingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My Anime Schedule API");
    options.RoutePrefix = "api-docs";
});

app.Run();
