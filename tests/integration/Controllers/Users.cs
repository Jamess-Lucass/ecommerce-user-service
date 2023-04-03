using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class Users : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;
    private readonly string _path = "/api/v1/users";

    public Users(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "TestScheme", options => { });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetUsers_Returns200()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (db == null)
            {
                Assert.Fail("Database context cannot be null");
            }

            if (db.Users.Any())
            {
                db.Users.RemoveRange(db.Users);
            }

            db.Users.Add(new User
            {
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@email.com"
            });

            await db.SaveChangesAsync();
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "TestScheme");
        var response = await _httpClient.GetAsync(_path);
        var data = await response.Content.ReadFromJsonAsync<Response<UserDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(data);
        Assert.Single(data.Value!);
        Assert.Equal("John", data.Value?.FirstOrDefault()?.FirstName);
    }
}