using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StoreAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Login attempt for username: {Username}", model?.Username);

            if (model == null)
            {
                _logger.LogWarning("Login model is null");
                return BadRequest("Dữ liệu đăng nhập không hợp lệ");
            }

            // Giả lập kiểm tra thông tin đăng nhập
            if (model.Username == "admin" && model.Password == "123456")
            {
                var token = GenerateJwtToken(model.Username);
                _logger.LogInformation("Login successful for username: {Username}", model.Username);
                
                return Ok(new { 
                    token = token,  // lowercase để consistent
                    username = model.Username,
                    expires = DateTime.Now.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            
            _logger.LogWarning("Login failed for username: {Username}", model.Username);
            return Unauthorized("Thông tin đăng nhập không hợp lệ.");
        }

        private string GenerateJwtToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8), // Tăng thời gian expire
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class LoginModel
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
