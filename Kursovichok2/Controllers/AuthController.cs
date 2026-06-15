using Kursovichok2.Data;
using Kursovichok2.DTOs.Autentif; // Проверь, что путь к твоим DTO верный
using Kursovichok2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        //Регистрация нового пользователя
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrDto dto) // Или RegisterDto, как у тебя назван класс
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Проверка: есть ли уже такой email
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Пользователь с таким email уже существует" });

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role ?? "user",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new ServervOtvet // Или AuthResponseDto, как у тебя назван класс ответа
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            });
        }

        //Вход в систему
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный email или пароль" });

            var token = GenerateJwtToken(user);

            return Ok(new ServervOtvet // Или AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            });
        }

        //  Генерация JWT-токена (вспомогательный метод)
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60")),
                signingCredentials: creds
            );

            // 🔹 Получить данные текущего пользователя (Профиль)
            [HttpGet("profile")]
            [Authorize]
            async Task<ActionResult<UserProfileDto>> GetProfile()
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new UserProfileDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        Role = u.Role
                    })
                    .FirstOrDefaultAsync();

                if (user == null) return NotFound();

                return Ok(user);
            }

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}