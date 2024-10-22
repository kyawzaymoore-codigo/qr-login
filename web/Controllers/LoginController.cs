using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace web.Controllers;

public class LoginController : Controller
{
    private readonly HttpClient _httpClient;

    public LoginController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IActionResult> Websocket()
    {
        var response = await _httpClient.GetAsync("http://localhost:5080/api/qrlogin/generate");
        var qrSession = JsonConvert.DeserializeObject<QrSessionResponse>(await response.Content.ReadAsStringAsync());
        ViewBag.Token = qrSession.Token;
        ViewBag.ExpiresAt = qrSession.ExpiresAt;
        return View();
    }

    public async Task<IActionResult> SSE()
    {
        var response = await _httpClient.GetAsync("http://localhost:5080/api/qrlogin/generate");
        var qrSession = JsonConvert.DeserializeObject<QrSessionResponse>(await response.Content.ReadAsStringAsync());
        ViewBag.Token = qrSession.Token;
        ViewBag.ExpiresAt = qrSession.ExpiresAt;
        return View();
    }
}

public class QrSessionResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}