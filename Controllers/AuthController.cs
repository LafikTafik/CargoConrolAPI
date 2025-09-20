using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CCAPI.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: /api/auth/login
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .Include(u => u.Role) // ← Подгружаем связанную роль
                .FirstOrDefaultAsync(u => u.Email == model.Email && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный email или пароль" });

            var accessToken = GenerateJwtToken(user);

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(4);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                Role = user.Role.Name, 
                UserId = user.ID,
                Name = $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.ID == userId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                UserId = user.ID,
                Email = user.Email,
                Role = user.Role.Name,
                Name = $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"
            });
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        [Authorize]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(currentUserRoleClaim))
                return Forbid("Роль не определена");

            var targetRoleName = model.Role ?? "User";
            var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName);
            if (targetRole == null)
                return BadRequest("Недопустимая роль");

            // Проверка прав: только админ может создавать админов и модераторов


            if (targetRoleName == "Admin" || targetRoleName == "Moderator")
            {
                if (currentUserRoleClaim != "Admin")
                    return Forbid("Только администратор может создавать пользователей с этой ролью");
            }
            else
            {
                if (currentUserRoleClaim != "Admin" && currentUserRoleClaim != "Moderator")
                    return Forbid("У вас нет прав для создания пользователей");
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && !u.IsDeleted))
                return BadRequest(new { message = "Email уже зарегистрирован" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                Email = model.Email,
                PasswordHash = passwordHash,
                RoleId = targetRole.Id, // ← Сохраняем ID роли
                ClientID = model.ClientID,
                DriverID = model.DriverID,
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Регистрация прошла успешно" });
        }

        // POST: /api/auth/refresh
        [EnableRateLimiting("RefreshPolicy")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = GetPrincipalFromExpiredToken(model.AccessToken);
            if (principal == null)
                return BadRequest("Invalid access token");

            var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp);
            if (expClaim == null || !long.TryParse(expClaim.Value, out long exp) || DateTimeOffset.FromUnixTimeSeconds(exp) > DateTimeOffset.UtcNow)
            {
                return BadRequest("Access token is not expired");
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                return BadRequest("Invalid user ID in token");

            var user = await _context.Users
                .Include(u => u.Role) // ← Подгружаем роль
                .FirstOrDefaultAsync(u => u.ID == id);

            if (user == null ||
                user.RefreshToken != model.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid refresh token");
            }

            var newAccessToken = GenerateJwtToken(user);

            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                Role = user.Role.Name, 
                UserId = user.ID,
                Name = $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"
            });
        }

        // POST: /api/auth/logout
        [HttpPost("logout")]
        [Authorize] // ← Добавлено: только авторизованные могут выйти
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} logged out and refresh token revoked", userId);
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
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.Client?.Name ?? user.Driver?.LastName ?? user.Email.Split('@')[0]}"),
                new Claim(ClaimTypes.Role, user.Role.Name) // ← Исправлено: передаём имя роли
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(10),
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
                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating expired token");
                return null;
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Роль обязательна")]
        public string Role { get; set; } = "User";

        public int? ClientID { get; set; }
        public int? DriverID { get; set; }
    }

    public class RefreshModel
    {
        [Required(ErrorMessage = "Access token обязателен")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refresh token обязателен")]
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