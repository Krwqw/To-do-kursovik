using Kursovichok2.Data;//подключение библиотек
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Kursovichok2.Services;

var builder = WebApplication.CreateBuilder(args);//начало приложения

//регистрация фоновый сервис уведомлений
builder.Services.AddHostedService<DeadlineNotificationService>();


//подключение к бд (MySQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

//настройка JWT аутентификации (читаются настройки для секретного ключ)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");//автоматически собирает данные из файлов конфигурации и ищет в файле конфигурации блок с именем JwtSettings
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);//метод переводит обычную текстовую строку в массив байтов, используя кодировку UTF-8

builder.Services.AddAuthentication(options =>//настройка системы проверки jwt пропуска
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

//настройка CORS (Cross-Origin Resource Sharing) для разрешения запросов от фронтенда
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000", 
            "http://localhost:5173",   
            "http://127.0.0.1:5500"   
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});


builder.Services.AddControllers(); //добавляем контроллеры (обработчики запросов)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => //добавляем документацию API
{
    c.SwaggerDoc("v1", new() { Title = "Task Manager API", Version = "v1" });

    //добавляем поддержку JWT в swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите JWT токен: Bearer {your-token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build(); //конец настройки приложения

//раздача готовых файлов сайта (HTML, CSS, JS)
// Это позволит открывать сайт по адресу https://localhost:7029


//Middleware, настройка swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Manager API v1");
    });
}


app.UseDefaultFiles(); // Ищет index.html по умолчанию
app.UseStaticFiles();  // Разрешает доступ к файлам из папки wwwroot

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();//перенаправление HTTP → HTTPS
app.UseCors("AllowFrontend");//проверка cors
app.UseAuthentication();//проверка jwt токена
app.UseAuthorization();//проверка авторизации
app.MapControllers();//вызов контроллера

app.Run();//запуск приложения