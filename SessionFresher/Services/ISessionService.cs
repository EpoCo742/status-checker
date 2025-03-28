namespace SessionFresher.Services
{
    public interface ISessionService
    {
        Guid GetSessionId();
        DateTime GetSessionExpirationDateTime();
    }
}
