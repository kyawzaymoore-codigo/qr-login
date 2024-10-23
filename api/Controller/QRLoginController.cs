using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[ApiController]
[Route("api/[controller]")]
public class QRLoginController : ControllerBase
{
    private readonly IQRLoginService _qrLoginService;
    private readonly ITokenService _tokenService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5); // Set default timeout
    public QRLoginController(IQRLoginService qrLoginService, ITokenService tokenService)
    {
        _qrLoginService = qrLoginService;
        _tokenService = tokenService;
    }

    [HttpGet("generate")]
    public IActionResult GenerateQRCode()
    {
        var session = _qrLoginService.GenerateSession();
        return Ok(new { token = session.Token, expiresAt = session.ExpiresAt });
    }

    [Authorize]
    [HttpPost("authenticate")]
    public IActionResult Authenticate([FromBody] QRLoginRequest request)
    {
        var session = _qrLoginService.GetSession(request.Token);
        if (session == null || session.ExpiresAt < DateTime.Now)
        {
            return BadRequest(new { success = false, message = "QR Code expired" });
        }

        _qrLoginService.Authenticate(request.Token);
        return Ok(new { success = true });
    }
    
    [HttpGet("status")]
    public async Task Status([FromQuery] string token)
    {
        Console.WriteLine("SSE status");
        if (string.IsNullOrEmpty(token))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("Token is required.");
            return;
        }

        HttpContext.Response.Headers.Add("Content-Type", "text/event-stream");
        HttpContext.Response.Headers.Add("Cache-Control", "no-cache");
        HttpContext.Response.Headers.Add("Connection", "keep-alive");

        var cancellationToken = HttpContext.RequestAborted;

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"cancellationToken : {cancellationToken.IsCancellationRequested}");
            var session = _qrLoginService.GetSession(token);

            if (session == null || DateTime.Now > session.ExpiresAt)
            {
                Console.WriteLine("QR Token Expired");
                await HttpContext.Response.WriteAsync($"data: {{\"isAuthenticated\": false, \"error\": \"QR Code Expired\"}}\n\n");
                await HttpContext.Response.Body.FlushAsync(cancellationToken);
                break; // Break the loop and stop the SSE connection
            }

            Console.WriteLine("Authenticated : " + session.IsAuthenticated);

            if (session != null && session.IsAuthenticated)
            {
                Console.WriteLine("QR Authenticated ", JsonConvert.SerializeObject(session));
                Console.WriteLine("QR Authenticated");
                string accessToken = _tokenService.GenerateJwtToken("kzm@gmail.com");
                await HttpContext.Response.WriteAsync($"data: {{\"isAuthenticated\": true, \"token\": \"{accessToken}\"}}\n\n");
                await HttpContext.Response.Body.FlushAsync(cancellationToken);
                return;
            }

            await Task.Delay(2000, cancellationToken); // Poll every 2 seconds
            Console.WriteLine("SSE Poll " + DateTime.Now);
        }
    }
}

public class QRLoginRequest
{
    public string Token { get; set; }
}