using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Task
{
    public class CreateTaskDto
    {
        //Создание задачи
        [Required(ErrorMessage = "Название задачи обязательно")]
        [StringLength(100, ErrorMessage = "Максимум 100 символов")]
        public string Title { get; set; } = string.Empty; //Название, обязательно

        [StringLength(1000)]
        public string? Description { get; set; } //описание до 1000 символов

        [RegularExpression(@"^(todo|inprogress|done)$", ErrorMessage = "Допустимые статусы: todo, inprogress, done")]
        public string Status { get; set; } = "todo"; /*Статус по умолчанию "todo". Регуляр ограничивает ввод 
                                                       только допустимыми значениями*/

        public DateTime? DueDate { get; set; } //дата дедлайна

        [Required(ErrorMessage = "ID доски обязателен")]
        public int BoardId { get; set; } //айди доски
    }
}
