using App.Data;
using App.Exceptions;
using App.Models;
using App.Services;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using System.Text;



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

        string jwtSecret = Environment.GetEnvironmentVariable("JwtSecret") ?? 
            throw new Exception("JwtSecret not set in .ENV");
        string jwtIssuer = Environment.GetEnvironmentVariable("JwtIssuer") ?? throw new Exception("JwtIssuer not set in .ENV");
        string jwtAudience = Environment.GetEnvironmentVariable("JwtAudience") ?? throw new Exception("JwtAudience not set in .ENV");

        builder.Configuration["JwtSecret"] = jwtSecret;
        builder.Configuration["JwtIssuer"] = jwtIssuer;
        builder.Configuration["JwtAudience"] = jwtAudience;

        // Add services to the container.
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, new MariaDbServerVersion(new Version(9, 0, 0)));
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddIdentityApiEndpoints<UserModel>(options => options.SignIn.RequireConfirmedAccount = false).AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Lösenordets struktur.
        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 16;
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorWasm", policy =>
            {
                policy.WithOrigins("http://localhost:5285")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);

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

        builder.Services.AddHostedService<AnimeIndexingBGService>();

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<UserManagementService>();
        builder.Services.AddScoped<PostManagementService>();
        builder.Services.AddScoped<ScheduleService>();

        var app = builder.Build();

        // Ser till att databasen är klar.
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
        }

        app.UseCors("AllowBlazorWasm");

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection(); 

        app.UseAuthentication();

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

        app.UseMiddleware<GlobalExceptionHandler>();

        app.Run();
    }
}

