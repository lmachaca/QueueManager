using Microsoft.EntityFrameworkCore;

namespace Listing.Data;

// Singleton shared by every browser connection. All changes go through here,
// and the Changed event tells every open page to reload from the database.
public class QueueService(IDbContextFactory<QueueDbContext> dbFactory)
{
    // SQLite allows one writer at a time; this also keeps ticket numbers unique per station
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public event Func<Task>? Changed;

    public async Task<List<Station>> GetStationsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Stations
            .AsNoTracking()
            .Include(s => s.Queue.OrderBy(p => p.TicketNumber))
            .OrderBy(s => s.Id)
            .ToListAsync();
    }

    public Task AddStationAsync(string name) => WriteAsync(db =>
    {
        db.Stations.Add(new Station { StationName = name });
        return Task.CompletedTask;
    });

    public Task DeleteStationAsync(int stationId) => WriteAsync(db =>
        db.Stations.Where(s => s.Id == stationId).ExecuteDeleteAsync());

    public Task AddPersonAsync(int stationId, string name) => WriteAsync(async db =>
    {
        var station = await db.Stations.FindAsync(stationId);
        if (station is null) return; // deleted from another browser

        db.People.Add(new Person
        {
            StationId = stationId,
            Name = name,
            TicketNumber = station.LocalTicketCounter++ // Increments only THIS card's counter
        });
    });

    public Task RemovePersonAsync(int personId) => WriteAsync(db =>
        db.People.Where(p => p.Id == personId).ExecuteDeleteAsync());

    private async Task WriteAsync(Func<QueueDbContext, Task> change)
    {
        await writeLock.WaitAsync();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await change(db);
            await db.SaveChangesAsync();
        }
        finally
        {
            writeLock.Release();
        }

        if (Changed is { } handlers)
        {
            foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
            {
                try { await handler(); }
                catch { /* a disconnected browser shouldn't break the others */ }
            }
        }
    }
}
