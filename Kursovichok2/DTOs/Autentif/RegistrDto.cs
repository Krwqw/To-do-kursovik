using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Autentif
{
    public class RegistrDto
    {
        //для регистрации надо:
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(50, MinimumLength = 5)]
        public string UserName { get; set; } = string.Empty; //имя пользователя, от 5 до 50 символов

        [Required(ErrorMessage = "Почта обязательно")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; //почта пользователя для входа

        [Required(ErrorMessage = "Пароль пользователя обязательно")]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty; //пароль без хеширования, от 6 до 100 символов

        public string Role { get; set; } = "user"; //базовая роль

        //[Required] - обязательно
        //[EmailAddress] - проверяет формат лялял@лялля.лялял
    }
}
