using Microsoft.EntityFrameworkCore;

namespace Listing.Data;

public class Station
{
    public int Id { get; set; }
    public string StationName { get; set; } = string.Empty;

    // Each station card maintains its own counter starting at 1
    public int LocalTicketCounter { get; set; } = 1;

    public List<Person> Queue { get; set; } = new List<Person>();
}

public class Person
{
    public int Id { get; set; }
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TicketNumber { get; set; }
}

public class QueueDbContext(DbContextOptions<QueueDbContext> options) : DbContext(options)
{
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Person> People => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Station>()
            .HasMany(s => s.Queue)
            .WithOne()
            .HasForeignKey(p => p.StationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
