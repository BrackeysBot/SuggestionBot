using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuggestionBot.Data;

namespace SuggestionBot.Services;

/// <summary>
///     Represents a service which connects to the SuggestionBot database.
/// </summary>
internal sealed class DatabaseService : BackgroundService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IDbContextFactory<SuggestionContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The DbContext factory.</param>
    public DatabaseService(ILogger<DatabaseService> logger, IDbContextFactory<SuggestionContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return CreateDatabaseAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        Directory.CreateDirectory("data");

        await using SuggestionContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        _logger.LogInformation("Creating database");
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}
