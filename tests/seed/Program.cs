using Bogus;
using Microsoft.EntityFrameworkCore;

Randomizer.Seed = new Random(8675309);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

Console.WriteLine($"Connecting to database {dbName} on server {dbHost},{dbPort} as user {dbUsername}");

string connectionString = $"Server={dbHost},{dbPort};Database={dbName};User={dbUsername};Password={dbPassword};MultipleActiveResultSets=true;TrustServerCertificate=true";

var options = new DbContextOptionsBuilder<ApplicationDbContext>();
options.UseSqlServer(connectionString).UseSnakeCaseNamingConvention();

using (var context = new ApplicationDbContext(options.Options))
{
    context.Database.EnsureCreated();

    if (context.Users.Any())
    {
        Console.WriteLine("Table is not empty");
        return;
    }

    var users = new Faker<User>()
        .RuleFor(x => x.Firstname, f => f.Person.FirstName)
        .RuleFor(x => x.Lastname, f => f.Person.LastName)
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.AvatarUrl, f => f.Internet.Avatar());

    context.Users.AddRange(users.Generate(100_000));

    context.SaveChanges();

    Console.WriteLine("Seed complete!");
}