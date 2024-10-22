using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

public class QRSession
{
    public string Token { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public interface IQRLoginService
{
    QRSession GenerateSession();
    QRSession GetSession(string token);
    void Authenticate(string token);
}

public class QRLoginService : IQRLoginService
{
    private readonly IMemoryCache _cache;  // or use Redis
    public QRLoginService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public QRSession GenerateSession()
    {
        var token = Guid.NewGuid().ToString();
        var session = new QRSession
        {
            Token = token,
            IsAuthenticated = false,
            ExpiresAt = DateTime.Now.AddMinutes(1)
        };

        _cache.Set(token, session, TimeSpan.FromMinutes(1));
        return session;
    }

    public QRSession GetSession(string token)
    {
        _cache.TryGetValue(token, out QRSession session);
        return session;
    }

    public void Authenticate(string token)
    {
        if (_cache.TryGetValue(token, out QRSession session))
        {
            session.IsAuthenticated = true;
            _cache.Set(token, session);
        }
    }
}