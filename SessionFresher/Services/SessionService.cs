using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class SessionService : BackgroundService, ISessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly SessionServiceOptions _options;

    private Guid _sessionId;
    private DateTime _sessionExpirationUtc;
    private DateTime _lastRefreshUtc = DateTime.MinValue;

    private readonly object _lock = new(); // For safely updating session state
    private readonly SemaphoreSlim _refreshLock = new(1, 1); // For coordinating refresh requests
    private Task<Guid>? _refreshTask;

    private readonly TimeSpan _refreshCooldown = TimeSpan.FromSeconds(10); // Prevent back-to-back refreshes

    public SessionService(ILogger<SessionService> logger, IOptions<SessionServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Guid GetSessionId()
    {
        lock (_lock)
        {
            return _sessionId;
        }
    }

    public DateTime GetSessionExpirationUtc()
    {
        lock (_lock)
        {
            return _sessionExpirationUtc;
        }
    }

    /// <summary>
    /// Called by consumers to explicitly refresh the session.
    /// Deduplicates concurrent refresh requests and prevents back-to-back refreshes.
    /// </summary>
    public async Task<Guid> RefreshSessionIdAsync()
    {
        await _refreshLock.WaitAsync();

        try
        {
            // If there's no refresh running or the last one finished
            if (_refreshTask == null || _refreshTask.IsCompleted)
            {
                // Skip refresh if a recent one just completed (cooldown)
                if (DateTime.UtcNow - _lastRefreshUtc < _refreshCooldown)
                {
                    _logger.LogInformation("Skipping refresh: recently refreshed within cooldown window.");
                    return GetSessionId();
                }

                // Start a new refresh task
                _refreshTask = InternalRefreshAsync();
            }
        }
        finally
        {
            _refreshLock.Release();
        }

        // All callers await the same task
        return await _refreshTask;
    }

    /// <summary>
    /// Performs the actual refresh logic and updates the session ID.
    /// </summary>
    private async Task<Guid> InternalRefreshAsync()
    {
        _logger.LogInformation("Refreshing session ID from remote source...");
        await Task.Delay(500); // Simulate remote latency

        var newSessionId = Guid.NewGuid(); // Simulate a fetched session ID

        lock (_lock)
        {
            _sessionId = newSessionId;
            _sessionExpirationUtc = DateTime.UtcNow.AddMinutes(_options.RefreshIntervalMinutes);
            _lastRefreshUtc = DateTime.UtcNow;
        }

        _logger.LogInformation("Session ID refreshed: {SessionId} (expires at {Expiration})",
            _sessionId.ToString()[..8], _sessionExpirationUtc);

        return newSessionId;
    }

    /// <summary>
    /// Background loop that refreshes the session on a fixed interval.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshSessionIdAsync();
                await Task.Delay(TimeSpan.FromMinutes(_options.RefreshIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background session refresh failed.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Retry after delay
            }
        }
    }

    /// <summary>
    /// Runs once at service startup to initialize the session ID.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SessionService starting...");
        await RefreshSessionIdAsync(); // Load initial session
        await base.StartAsync(cancellationToken);
    }
}
