using DjAssistantAgenticDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace DjAssistantAgenticDemo.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PlayedSong> PlayedSongs => Set<PlayedSong>();
    public DbSet<Suggestion> Suggestions => Set<Suggestion>();
    public DbSet<PlayedSongAudit> PlayedSongAudits => Set<PlayedSongAudit>();
}
