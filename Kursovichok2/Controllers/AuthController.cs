using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Autentif;
using Kursovichok2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kursovichok2.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ServervOtvet>> Register(RegistrDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var userName = dto.UserName.Trim();

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict(new { message = "Пользователь с такой почтой уже зарегистрирован." });
            }

            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = HashPassword(dto.Password),
                Role = NormalizeRole(dto.Role)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(CreateAuthResponse(user));
        }

        [HttpPost("login")]
        public async Task<ActionResult<ServervOtvet>> Login(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null || !VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Неверная почта или пароль." });
            }

            return Ok(CreateAuthResponse(user));
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<ViewProfile>> Profile()
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);

            if (user is null)
            {
                return NotFound(new { message = "Пользователь не найден." });
            }

            return new ViewProfile
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            };
        }

        private ServervOtvet CreateAuthResponse(User user)
        {
            return new ServervOtvet
            {
                Token = CreateToken(user),
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            };
        }

        private string CreateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        private static string NormalizeRole(string? role)
        {
            return role?.Trim().ToLowerInvariant() switch
            {
                "admin" => "admin",
                "manager" => "manager",
                _ => "user"
            };
        }

        private static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = SHA256.HashData(salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray());
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string savedHash)
        {
            var parts = savedHash.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            var actualHash = SHA256.HashData(salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray());
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
