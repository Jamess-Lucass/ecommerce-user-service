using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService? _user;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService user) : base(options)
    {
        _user = user;
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Base> entry in ChangeTracker.Entries<Base>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.CreatedBy = _user?.Id ?? Guid.Empty;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.UpdatedBy = _user?.Id ?? Guid.Empty;
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    public override int SaveChanges()
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Base> entry in ChangeTracker.Entries<Base>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.Now;
                    entry.Entity.CreatedBy = _user?.Id ?? Guid.Empty;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.Now;
                    entry.Entity.UpdatedBy = _user?.Id ?? Guid.Empty;
                    break;
            }
        }

        var result = base.SaveChanges();

        return result;
    }
}