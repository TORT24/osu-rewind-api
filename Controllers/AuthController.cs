using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace ORewindApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromQuery] LoginModel user)
    {
        string login = _configuration["Auth:Login"]!;
        string password = _configuration["Auth:Password"]!;
        string secretKeyString = _configuration["Auth:Key"]!;
        if (user.UserName == login && user.Password == password)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKeyString));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            var tokeOptions = new JwtSecurityToken(
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(1),
                claims: new List<Claim>(),
                signingCredentials: signinCredentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);

            return Ok(new AuthenticatedResponse { Token = tokenString, ValidUntill = DateTime.UtcNow.AddDays(1) });
        }
        else
            return BadRequest("Invalid client request");
    }

    public class LoginModel
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class AuthenticatedResponse
    {
        public string? Token { get; set; }
        public DateTime? ValidUntill { get; set; }
    }
}