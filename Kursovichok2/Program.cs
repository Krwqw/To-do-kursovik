using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Kursovichok2.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 1. Подключение к базе данных (MySQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// 🔹 2. Настройка JWT аутентификации
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// 🔹 3. CORS (разрешаем запросы от фронтенда)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",   // React (create-react-app)
            "http://localhost:5173",   // Vite (Vue/React/Svelte)
            "http://127.0.0.1:5500"    // Live Server (VS Code)
        )
        .AllowAnyHeader()              // Разрешаем заголовки (Content-Type, Authorization)
        .AllowAnyMethod()              // Разрешаем GET, POST, PUT, DELETE
        .AllowCredentials();           // Разрешаем передачу токенов
    });
});

// 🔹 4. Контроллеры + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 5. Middleware (порядок важен!)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ⚠️ CORS должен быть ДО UseAuthentication и UseAuthorization
app.UseCors("AllowFrontend");

app.UseAuthentication();  // Проверка токена
app.UseAuthorization();   // Проверка прав [Authorize]
app.MapControllers();     // Маршруты API

app.Run();
