using Listing.Components;
using Listing.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQLite database shared by every browser
builder.Services.AddDbContextFactory<QueueDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("QueueDb")));
builder.Services.AddSingleton<QueueService>();

var app = builder.Build();

// Create the database folder (e.g. /home/data on Azure), file and tables on first run
var dbPath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("QueueDb")).DataSource;
var dbFolder = Path.GetDirectoryName(Path.GetFullPath(dbPath));
if (!string.IsNullOrEmpty(dbFolder))
{
    Directory.CreateDirectory(dbFolder);
}

using (var db = app.Services.GetRequiredService<IDbContextFactory<QueueDbContext>>().CreateDbContext())
{
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
