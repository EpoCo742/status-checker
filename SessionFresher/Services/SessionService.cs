using Microsoft.Extensions.Options;
using SessionFresher.Config;

namespace SessionFresher.Services
{

    public class SessionService : BackgroundService, ISessionService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SessionServiceOptions _options;
        private Guid _sessionId;
        private readonly object _lock = new();
        private DateTime _sessionExpirationUtc;

        public SessionService(ILogger<SessionService> logger,
                              IOptions<SessionServiceOptions> options,
                              IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        public Guid GetSessionId()
        {
            lock (_lock)
            {
                return _sessionId;
            }
        }

        private static async Task<Guid> RefreshSessionIdAsync()
        {
            // Simulate remote fetch
            await Task.Delay(500); // simulate latency
            return Guid.NewGuid();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var newSessionId = await RefreshSessionIdAsync();
                    lock (_lock)
                    {
                        _sessionId = newSessionId;
                        _sessionExpirationUtc = DateTime.UtcNow.AddMinutes(_options.RefreshIntervalMinutes);
                    }
                    _logger.LogInformation($"Session ID refreshed: {_sessionId}, Expires at: {_sessionExpirationUtc:u}");

                    await Task.Delay(TimeSpan.FromMinutes(_options.RefreshIntervalMinutes), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh session ID");
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _sessionId = await RefreshSessionIdAsync();
            _sessionExpirationUtc = DateTime.UtcNow.AddMinutes(_options.RefreshIntervalMinutes);
            _logger.LogInformation($"Initial session ID: {_sessionId}, Expires at: {_sessionExpirationUtc:u}");
            await base.StartAsync(cancellationToken);
        }

        public DateTime GetSessionExpirationDateTime()
        {
            lock (_lock)
            {
                return _sessionExpirationUtc;
            }
        }
    }

}
