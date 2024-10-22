using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{

    private readonly ITokenService _tokenService;
    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Hardcoded credentials (for simplicity)
        if (request.Email == "kzm@gmail.com" && request.Password == "password")
        {
            var token = _tokenService.GenerateJwtToken(request.Email);
            return Ok(new { token });
        }

        return Unauthorized();
    }

    [Authorize]
    [HttpGet("verify")]
    public IActionResult Verify()
    {
        return Ok();
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}