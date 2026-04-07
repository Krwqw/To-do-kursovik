using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Autentif
{
    public class LoginDto
    {
        //для входа нужны только почта и пароль
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
