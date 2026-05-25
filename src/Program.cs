using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OutsourceTracker.Authentication;
using OutsourceTracker.BusinessUnit.Accounts;
using OutsourceTracker.BusinessUnit.Divisions;
using OutsourceTracker.Data;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Services;
using OutsourceTracker.Services.ModelService;
using SendGrid.Extensions.DependencyInjection;
using System.Text;

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

        builder.Services.AddSendGrid(options =>
        {
            options.ApiKey = builder.Configuration["SendGrid:ApiKey"]!;
            options.SetDataResidency("global");
        });
        builder.Services.AddScoped<EmailService>();

        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDataContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });


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

        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddScoped<OrganizationalUnitService>();
        builder.Services.AddScoped<AccountService>();
        builder.Services.AddScoped<TrailerService>();
        builder.Services.AddScoped<ZoneDataService>();

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
