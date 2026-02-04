using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using server;
using StackExchange.Redis;
using StateleSSE.AspNetCore;
using StateleSSE.AspNetCore.Extensions;

namespace server;

public class Program
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // =========================
        // AppOptions (for JWT config)
        // =========================
        services.AddSingleton<AppOptions>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var appOptions = new AppOptions();
            configuration.GetSection(nameof(AppOptions)).Bind(appOptions);
            return appOptions;
        });
        
        services.AddDbContext<AppDbContext>((services, options) =>
        {
            options.UseNpgsql(services.GetRequiredService<AppOptions>().DbConnectionString);
        });

        // =========================
        // JWT Authentication
        // =========================
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IServiceProvider>((options, sp) =>
            {
                var appOptions = sp.GetRequiredService<AppOptions>();
                var key = Encoding.UTF8.GetBytes(appOptions.Token);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = appOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = appOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true
                };
            });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // =========================
        // SSE + Backplane
        // =========================
        services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(0);
        });

        services.AddInMemorySseBackplane();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(
                "frankfurt-keyvalue.render.com:6379,user=red-d5tia7npm1nc739csn40,password=f0UwYPpRxHH2BLVBXkBCEhFOnW1caOzp,ssl=true,abortConnect=false"
            );
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddRedisSseBackplane();

        // =========================
        // ASP.NET basics
        // =========================
        services.AddControllers();
        services.AddOpenApi();
        services.AddOpenApiDocument();
        services.AddCors();
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);

        var app = builder.Build();

        // =========================
        // Validate AppOptions
        // =========================
        var appOptions = app.Services.GetRequiredService<AppOptions>();
        Validator.ValidateObject(appOptions, new ValidationContext(appOptions), true);

        // =========================
        // Middleware pipeline
        // =========================
        app.UseOpenApi();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseCors(policy =>
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowAnyOrigin()
                  .SetIsOriginAllowed(_ => true)
        );

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // =========================
        // SSE disconnect handling
        // =========================
        var backplane = app.Services.GetRequiredService<ISseBackplane>();
        backplane.OnClientDisconnected += async (_, e) =>
        {
            await backplane.Clients.SendToGroupsAsync(
                e.Groups,
                new { message = "Someone disconnected!" }
            );
        };
        
        app.GenerateApiClientsFromOpenApi("../client/src/generated-ts-client.ts", "./openapi.json").GetAwaiter().GetResult();

        app.Run();
    }
}