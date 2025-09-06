using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CCAPI.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный email или пароль" });

            // Генерируем Access Token (2 часа)
            var accessToken = GenerateJwtToken(user);

            // Генерируем Refresh Token (7 дней)
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                Role = user.Role,
                UserId = user.ID,
                Name = $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new
            {
                UserId = int.Parse(userId),
                Email = email,
                Role = role,
                Name = name
            });
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && !u.IsDeleted))
                return BadRequest(new { message = "Email уже зарегистрирован" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                Email = model.Email,
                PasswordHash = passwordHash,
                Role = model.Role ?? "User",
                ClientID = model.ClientID,
                DriverID = model.DriverID,
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Регистрация прошла успешно" });
        }

        // POST: /api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshModel model)
        {
            var principal = GetPrincipalFromExpiredToken(model.AccessToken);
            if (principal == null) return BadRequest("Invalid access token");

            var userId = principal.Identity.Name;
            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null ||
                user.RefreshToken != model.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid refresh token");
            }

            // Генерируем новый Access Token
            var newAccessToken = GenerateJwtToken(user);

            // Генерируем новый Refresh Token
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                Role = user.Role,
                UserId = user.ID,
                Name = $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"
            });
        }

        // POST: /api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }
            return Ok("Logged out");
        }

        // Метод для генерации JWT токена
        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Метод для генерации Refresh Token
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // Метод для извлечения Claims из истёкшего токена
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string jwtToken)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(jwtToken, tokenValidationParameters, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token");

                return principal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating expired token: {ex.Message}");
                return null;
            }
        }

        // Вспомогательные методы
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        }
    }

    // Модели
    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
        public int? ClientID { get; set; }
        public int? DriverID { get; set; }
    }

    public class RefreshModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Role { get; set; } = "User";
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}