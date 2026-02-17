using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using OutsourceTracker.Data;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
        });

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), JwtBearerDefaults.AuthenticationScheme);

        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        });

        builder.Services.AddDbContext<AppDataContext>(options =>
        {
            var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            if (builder.Environment.IsDevelopment())
            {
                options.UseSqlite(connStr);
                options.EnableSensitiveDataLogging();
            }
            else
            {
                options.UseSqlite(connStr);
            }
        });

        builder.Services.AddHostedService<AppDataContextInitializer>();

        builder.Services.AddScoped<TrailerDataService>()
            .AddScoped<IModelCreateService<Trailer>>(s => s.GetRequiredService<TrailerDataService>())
            .AddScoped<IModelDeleteService<Trailer>>(s => s.GetRequiredService<TrailerDataService>())
            .AddScoped<IModelUpdateService<Trailer>>(s => s.GetRequiredService<TrailerDataService>())
            .AddScoped<IModelLookupService<Trailer>>(s => s.GetRequiredService<TrailerDataService>());

        var app = builder.Build();
        app.UseCors();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
