using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnightCore.Infrastructure;

// Schema generation must not depend on production secrets or start background jobs.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer("Server=localhost;Database=KnightCore;Integrated Security=True;TrustServerCertificate=True")
        .Options);
}
