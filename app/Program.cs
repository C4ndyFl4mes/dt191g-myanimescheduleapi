using App.Data;
using App.Models;
using App.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;



namespace App;

public class Program
{
    public static async Task Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHttpClient();

        var env = builder.Environment;
        Env.Load("../.env");

        string moderatorEmail = Environment.GetEnvironmentVariable("ModeratorEmail") ?? throw new Exception("No primary moderator email provided in .ENV.");
        string moderatorPassword = Environment.GetEnvironmentVariable("ModeratorPassword") ?? throw new Exception("No primary moderator password provided in .ENV.");
        string moderatorUsername = Environment.GetEnvironmentVariable("ModeratorUsername") ?? throw new Exception("No primary moderator username provided in .ENV.");

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

        builder.Services.AddAuthorization();

        builder.Services.AddIdentityApiEndpoints<UserModel>(options => options.SignIn.RequireConfirmedAccount = false).AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "My Anime Schedule API",
                Version = "v1"
            });
        });
        
        builder.Services.AddHostedService<AnimeIndexingService>();
        
        builder.Services.AddScoped<ScheduleService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();


        // Lägger till roller i databasen.
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            string[] roles = ["Moderator", "Member"];

            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {

                    IdentityResult result = await roleManager.CreateAsync(new IdentityRole<int>(role));
                    if (!result.Succeeded)
                    {
                        string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create role '{role}': {errors}");
                    }
                }
            }
        }

        // Lägger till en moderator i databasen.
        using (var scope = app.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserModel>>();

            if (await userManager.FindByEmailAsync(moderatorEmail) == null)
            {
                UserModel moderatorUser = new()
                {
                    UserName = moderatorUsername,
                    Email = moderatorEmail
                };
                
                IdentityResult createResult = await userManager.CreateAsync(moderatorUser, moderatorPassword);
                if (!createResult.Succeeded)
                {
                    string errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create moderator user: {errors}");
                }

                var createdUser = await userManager.FindByEmailAsync(moderatorEmail) ?? throw new Exception("Failed to find the created moderator user.");
                await userManager.AddToRoleAsync(createdUser, "Moderator");
            }
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "My Anime Schedule API");
            options.RoutePrefix = "api-docs";
        });

        app.Run();
    }
}

