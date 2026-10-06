using System.Security.Claims;
using System.Text.Json;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ProfilesApi.API.Constants;
using ProfilesApi.API.Middleware;
using ProfilesApi.Application;
using ProfilesApi.Application.Consumers;
using ProfilesApi.Infrastructure;
using Serilog;
using Serilog.Events;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("LuckyPennySoftware.AutoMapper", LogEventLevel.Error)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.HttpsPolicy", LogEventLevel.Error)
    .WriteTo.Console()
    .WriteTo.MongoDB(builder.Configuration.GetConnectionString("MongoLogging")
                     ?? throw new InvalidOperationException("Mongodb connection string is missing in configuration"))
    .CreateLogger();
builder.Host.UseSerilog();

try
{
    Log.Information("Starting web host...");
    builder.Configuration.AddEnvironmentVariables();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddOpenApi();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<PatientRegisteredConsumer>();
        x.AddConsumer<ProfileRegisteredConsumer>();
        x.UsingRabbitMq((context, cfg) =>
        {
            var rabbitSettings = builder.Configuration.GetSection("RabbitMQ");

            cfg.Host(
                rabbitSettings["Host"] ?? "localhost",
                rabbitSettings["VirtualHost"] ?? "/",
                h =>
                {
                    h.Username(rabbitSettings["Username"] ?? "guest");
                    h.Password(rabbitSettings["Password"] ?? "guest");
                }
            );

            cfg.ReceiveEndpoint("patient-registered-queue", e =>
            {
                e.ConfigureConsumer<PatientRegisteredConsumer>(context);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = builder.Environment.ApplicationName,
            Version = "v1"
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT token (without Bearer prefix)"
        };

        options.AddSecurityDefinition("Bearer", securityScheme);

        options.AddSecurityRequirement((doc) =>
        {
            var requirement = new OpenApiSecurityRequirement();
            var reference = new OpenApiSecuritySchemeReference("Bearer", doc);
            requirement[reference] = new List<string>();

            return requirement;
        });
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
        var keycloakRealm = builder.Configuration["Keycloak:Realm"];

        if (string.IsNullOrWhiteSpace(keycloakBaseUrl) || string.IsNullOrWhiteSpace(keycloakRealm))
            throw new InvalidOperationException("Keycloak configuration is missing.");

        var authority = $"{keycloakBaseUrl.TrimEnd('/')}/realms/{keycloakRealm}";

        options.Authority = authority;
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = false
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
                {
                    var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                        if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleName = role.GetString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.RequireAdmin, policy =>
            policy.RequireRole("Administrator"));

        options.AddPolicy(AuthPolicies.RequireStaff, policy =>
            policy.RequireRole("Administrator", "Doctor"));

        options.AddPolicy(AuthPolicies.RequirePatientOrAdmin, policy =>
            policy.RequireRole("Administrator", "Patient"));

        options.AddPolicy(AuthPolicies.RequireAllRoles, policy =>
            policy.RequireRole("Administrator", "Doctor", "Patient"));
    });

    var app = builder.Build();

    app.UseExceptionHandler();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ProfilesApi.Infrastructure.Data.AppDbContext>();
            context.Database.Migrate();
            Log.Information("Database migrated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}