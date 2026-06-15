using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Comment
{
    public class CreateCommDto
    {
        //создание комментария
        [Required(ErrorMessage = "Текст комментария не может быть пустым")]
        public string Text { get; set; } = string.Empty; //текст комментария

        [Required(ErrorMessage = "ID задачи обязателен")]
        public int TaskId { get; set; } //айди задачи
    }
}
