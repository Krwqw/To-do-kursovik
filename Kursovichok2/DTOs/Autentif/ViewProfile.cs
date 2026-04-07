using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Autentif
{
    public class ViewProfile
    {
        //что видно при просмотре профиля
        [Required]
        public int Id { get; set; } //айди пользователя, чей профиль просматривается

        [Required]
        public string UserName { get; set; } = string.Empty; //имя пользователя

        [Required]
        public string Email { get; set; } = string.Empty; //почта

        [Required]
        public string Role { get; set; } = string.Empty; //роль
    }
}
