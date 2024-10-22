using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class QRLoginWebSocketHandler
{
    private readonly IQRLoginService _qrLoginService;
    private readonly ITokenService _tokenService;

    public QRLoginWebSocketHandler(IQRLoginService qrLoginService,ITokenService tokenService)
    {
        _qrLoginService = qrLoginService;
        _tokenService = tokenService;
    }

    public async Task HandleWebSocket(HttpContext context)
    {
        Console.WriteLine("HandleWebSocket");
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var token = await ReceiveSessionToken(webSocket);

            // Start expiration check
            _ = CheckTokenExpiration(webSocket, token);

            // Listen for login status changes
            await ListenForMessages(webSocket, token);
        }
        else
        {
            context.Response.StatusCode = 400; // Bad Request
        }
    }

    private async Task ListenForMessages(WebSocket webSocket, string token)
    {
        Console.WriteLine("ListenForMessages");
        while (webSocket.State == WebSocketState.Open)
        {
            var session = _qrLoginService.GetSession(token);
            Console.WriteLine("Authenticated : " + session.IsAuthenticated);
            if (session != null && session.IsAuthenticated)
            {
                var message = new { isAuthenticated = true, token = _tokenService.GenerateJwtToken("kzm@gmail.com")};
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), WebSocketMessageType.Text, true, CancellationToken.None);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Login Successful", CancellationToken.None);
                return;
            }

            await Task.Delay(2000);  // Poll every 2 seconds
        }
    }

    private async Task CheckTokenExpiration(WebSocket webSocket, string token)
    {
        Console.WriteLine("CheckTokenExpiration");
        var session = _qrLoginService.GetSession(token);
        if (session == null) return;

        var timeRemaining = session.ExpiresAt - DateTime.Now;
        if (timeRemaining > TimeSpan.Zero)
        {
            await Task.Delay(timeRemaining);
        }

        if (webSocket.State == WebSocketState.Open)
        {
            Console.WriteLine("QR Code Expired");
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "QR Code Expired", CancellationToken.None);
        }
    }

    private async Task<string> ReceiveSessionToken(WebSocket webSocket)
    {
        Console.WriteLine("ReceiveSessionToken");
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
