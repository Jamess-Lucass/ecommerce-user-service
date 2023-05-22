using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Testcontainers.MsSql;

public class TestWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly MsSqlContainer _databaseContainer = new MsSqlBuilder().Build();

    public Task InitializeAsync()
    {
        return _databaseContainer.StartAsync();
    }

    public new Task DisposeAsync()
    {
        return _databaseContainer.DisposeAsync().AsTask();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_databaseContainer.GetConnectionString());
            });
        });

        return base.CreateHost(builder);
    }
}

// Response type for OData
public class Response<T>
{
    public IEnumerable<T>? Value { get; set; }
}