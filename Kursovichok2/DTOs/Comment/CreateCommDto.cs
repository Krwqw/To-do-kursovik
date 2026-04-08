using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Comment
{
    public class CreateCommDto
    {
        //создание комментария
        [Required]
        public string Text { get; set; } = string.Empty; //текст комментария

        [Required]
        public int TaskId { get; set; } //айди задачи
    }
}
