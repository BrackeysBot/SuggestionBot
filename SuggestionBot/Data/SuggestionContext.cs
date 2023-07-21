using Microsoft.EntityFrameworkCore;
using SuggestionBot.Data.EntityConfigurations;

namespace SuggestionBot.Data;

/// <summary>
///     Represents a session with the database.
/// </summary>
internal sealed class SuggestionContext : DbContext
{
    public DbSet<BlockedUser> BlockedUsers { get; internal set; } = null!;

    /// <summary>
    ///     Gets the set of suggestions.
    /// </summary>
    public DbSet<Suggestion> Suggestions { get; internal set; } = null!;

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=data/suggestions.db");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BlockedUserConfiguration());
        modelBuilder.ApplyConfiguration(new SuggestionConfiguration());
    }
}
