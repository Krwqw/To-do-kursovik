using Kursovichok2.Data;                      // Доступ к базе данных (AppDbContext)
using Kursovichok2.DTOs.Autentif;             // DTO для регистрации/входа (что приходит с фронта)
using Kursovichok2.Models;                    // Модель User
using Microsoft.AspNetCore.Authorization;     // Атрибут [Authorize] для защиты методов
using Microsoft.AspNetCore.Mvc;               // ControllerBase, HttpPost, Ok(), BadRequest() и т.д.
using Microsoft.EntityFrameworkCore;          // Методы EF: AnyAsync, FirstOrDefaultAsync
using Microsoft.IdentityModel.Tokens;         // Для создания JWT-токена (ключи, подпись)
using System.IdentityModel.Tokens.Jwt;        // Класс JwtSecurityToken
using System.Security.Claims;                 // ✅ Обязательно для ClaimTypes (данные внутри токена)
using System.Text;                            // Кодировка для секретного ключа

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]   // Базовый URL: api/auth
    [ApiController]               // Авто-валидация моделей и обработка ошибок
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;  // База данных
        private readonly IConfiguration _config; // Конфиг (секретный ключ JWT из appsettings.json)

        // Внедрение зависимостей: ASP.NET сам создаст и передаст context и config
        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

       
        [HttpPost("register")] //Регистрация нового пользователя
        public async Task<IActionResult> Register([FromBody] RegistrDto dto)
        {
            // Проверка валидности данных (обязательные поля заполнены, email в правильном формате)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Проверка: нет ли уже пользователя с таким email (защита от дубликатов)
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Пользователь с таким email уже существует" });

            // Создаём нового пользователя
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), //Хэшируем пароль (не храним в открытом виде!)
                Role = "user",                                               //Роль по умолчанию
                CreatedAt = DateTime.UtcNow                                  //Время создания (UTC)
            };

            _context.Users.Add(user);              // Добавляем в БД (в памяти)
            await _context.SaveChangesAsync();     // Сохраняем — теперь у user есть Id

            // Генерируем JWT-токен для автоматического входа после регистрации
            var token = GenerateJwtToken(user);

            // Возвращаем токен и данные пользователя (фронт сохранит токен)
            return Ok(new ServervOtvetDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            });
        }

        

        [HttpPost("login")]//Вход в систему
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Проверка валидности данных
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ищем пользователя по email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            // Проверяем: пользователь найден И пароль совпадает с хэшем в БД
            // BCrypt.Verify сравнивает введённый пароль с сохранённым хэшем
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный email или пароль" });

            // Генерируем JWT-токен
            var token = GenerateJwtToken(user);

            // Возвращаем токен и данные пользователя
            return Ok(new ServervOtvetDto
            {
                Token = token,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role
            });
        }

        

        [HttpGet("profile")]//Получить данные текущего пользователя
        [Authorize]  //только с авторизацией
        public async Task<ActionResult<ViewProfileDto>> GetProfile()
        {
            //Получаем ID напрямую из токена (без вспомогательного метода)
            // ASP.NET автоматически распаковывает токен в объект User
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Ищем пользователя по ID и сразу превращаем в DTO (без пароля!)
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new ViewProfileDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role
                })
                .FirstOrDefaultAsync();

            // Если пользователь не найден (маловероятно, но на всякий случай)
            if (user == null) return NotFound();

            return Ok(user);  // 200 OK с данными профиля
        }


        private string GenerateJwtToken(User user)  //метод генерации JWT (создаёт токен — "электронный пропуск" с данными пользователя)
        {
            // Берём настройки JWT из appsettings.json (секция "JwtSettings")
            var jwtSettings = _config.GetSection("JwtSettings");

            // Создаём симметричный ключ из секретной строки
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            // Указываем алгоритм подписи (HMAC SHA256)
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims — "вкладыши" в токен (данные о пользователе)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // ID пользователя
                new Claim(ClaimTypes.Email, user.Email),                   // Email
                new Claim(ClaimTypes.Name, user.UserName),                 // Имя
                new Claim(ClaimTypes.Role, user.Role)                      // Роль
            };

            // Собираем токен
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],           // Кто выдал токен
                audience: jwtSettings["Audience"],       // Для кого токен
                claims: claims,                          // Данные внутри
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60")), // Время жизни (по умолчанию 60 мин)
                signingCredentials: creds                // Подпись (защита от подделки)
            );

            // Превращаем токен в строку (длинная base64-строка)
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}