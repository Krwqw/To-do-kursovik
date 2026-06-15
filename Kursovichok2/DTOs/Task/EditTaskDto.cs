using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Task
{
    public class EditTaskDto
    {
        //редактирвоание задачи, все поля не обязательно, тк могуьт не редактироваться
        [StringLength(100, ErrorMessage = "Максимум 100 символов")]
        public string? Title { get; set; } //название

        [StringLength(1000, ErrorMessage = "Максимум 1000 символов")]
        public string? Description { get; set; } //описание

        [RegularExpression(@"^(todo|inprogress|done)$", ErrorMessage = "Допустимые статусы: todo, inprogress, done")]
        public string? Status { get; set; } //статус

        public DateTime? DueDate { get; set; } //дата создания
    }
}
