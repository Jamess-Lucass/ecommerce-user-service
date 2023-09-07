using System.Reflection;
using System.Text;
using API.Services;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
string connectionString = $"Server={dbHost},{dbPort};Database={dbName};User={dbUsername};Password={dbPassword};MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, options =>
    {
        options.EnableRetryOnFailure(3);

        options.CommandTimeout(30);
    }).UseSnakeCaseNamingConvention();

    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddCors();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<GoatQueryOpenAPIFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer ey.....\"",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    options.ResolveConflictingActions(x => x.First());
});

builder.Logging.ClearProviders();

builder.Host.UseSerilog((hostContext, services, configuration) =>
{
    configuration
    .Enrich.WithElasticApmCorrelationInfo()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Information)
    .WriteTo.Console(new EcsTextFormatter());
});

builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

var JWTSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (string.IsNullOrEmpty(JWTSecret))
{
    throw new ArgumentNullException("JWT_SECRET environment variable not set");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidIssuer = "",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JWTSecret)),
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("x-access-token"))
            {
                context.Token = context.Request.Cookies["x-access-token"];
            }

            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                // Override any cookie token is it's passed as the Authorization header
                string? authorization = context.Request.Headers["Authorization"].FirstOrDefault();

                if (!string.IsNullOrEmpty(authorization))
                {
                    context.Token = authorization.Substring("Bearer ".Length);
                }
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

// Services
builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserService, UserService>();

// Validators
builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();

// Search
builder.Services.AddSingleton<ISearchBinder<UserDto>, UserSearchBinder>();

var app = builder.Build();

app.UseAllElasticApm();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseCors(opt => opt
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(origin => true)
        .AllowCredentials()
    );

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "User Service - Swagger UI";

        options.DisplayRequestDuration();
    });

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();
    }
}
else
{
    string origins = Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "";

    app.UseCors(opt => opt
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins(origins.Split(","))
        .AllowCredentials()
    );

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapHealthChecks("/api/healthz");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, ex.Message);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { code = 500, message = "Unexpected error occurred, please try again or contact support" });
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
