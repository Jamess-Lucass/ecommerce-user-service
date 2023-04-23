using System.Reflection;
using System.Text;
using API.Services;
using Asp.Versioning.ApiExplorer;
using Elastic.CommonSchema.Serilog;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

builder.Services.AddControllers()
.AddOData(options =>
{
    options.RouteOptions.EnableKeyInParenthesis = false;
    options.RouteOptions.EnableNonParenthesisForEmptyParameterFunction = true;
    options.RouteOptions.EnableQualifiedOperationCall = false;
    options.RouteOptions.EnableUnqualifiedOperationCall = true;
    options.RouteOptions.EnableActionNameCaseInsensitive = true;
    options.Conventions.Remove(options.Conventions.OfType<MetadataRoutingConvention>().First());

    options.Count()
        .Filter()
        .Expand()
        .Select()
        .OrderBy()
        .SetMaxTop(1_000);
});
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning(opt => opt.ReportApiVersions = true)
.AddOData(options => options.AddRouteComponents("api/v{version:apiVersion}"))
.AddODataApiExplorer(options =>
{
    options.MetadataOptions = ODataMetadataOptions.None;
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

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

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
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
    if (builder.Environment.IsDevelopment())
    {
        configuration
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Information)
        .WriteTo.Console();
    }
    else
    {
        configuration
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft.Extensions.Http.DefaultHttpClientFactory", LogEventLevel.Information)
        .WriteTo.Console(new EcsTextFormatter());
    }
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseODataRouteDebug();

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

        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();
            options.SwaggerEndpoint(url, name);
        }
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
app.MapHealthChecks("/healthz");

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        await context.Response.WriteAsJsonAsync(new { code = 404, message = "No Resource Found" });
    }

    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        await context.Response.WriteAsJsonAsync(new { code = 401, message = "Not authenticated" });
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
