using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data;
using OutsourceTracker.Services.ModelService;

namespace OutsourceTracker.Backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
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
        builder.Services.AddScoped<TrailerDataService>();
        var app = builder.Build();
        app.UseCors();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
