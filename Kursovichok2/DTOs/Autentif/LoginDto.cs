using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Autentif
{
    public class LoginDto
    {
        //для входа нужны только почта и пароль
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }
}
