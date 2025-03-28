using Microsoft.Extensions.Logging;

namespace SessionFresher.Services
{
    public class StatusService(ISessionService sessionService, ILogger<SessionService> logger) : IStatusService
    {
        private readonly ISessionService _sessionService = sessionService;

        public bool IsHealthy()
        {
            var sessionId = _sessionService.GetSessionId();
            var sessionExpirationUtc = _sessionService.GetSessionExpirationDateTime();

            logger.LogInformation($"Status Requested: {sessionId}, Expires at: {sessionExpirationUtc:u}");

            return true;
        }
    }
}
