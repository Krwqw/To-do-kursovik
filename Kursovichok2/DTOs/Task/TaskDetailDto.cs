using Kursovichok2.DTOs.Comment;
namespace Kursovichok2.DTOs.Task
{
    public class TaskDetailDto
    {
        //детали задачи
        public int Id { get; set; }  //айди задачи
        public string Title { get; set; } = string.Empty; //название
        public string? Description { get; set; } //описание
        public string Status { get; set; } = string.Empty; //статус
        public DateTime? DueDate { get; set; } //дедлайн, не обязательно, если указан
        public DateTime CreatedAt { get; set; } //дата создания

        public List<CommentDto> Comments { get; set; } = new(); /*Вложенный список комментариев, 
                                                                  инициализация = new() создает пустой список*/
        public string? AssigneeName { get; set; }
    }
}
