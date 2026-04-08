namespace Kursovichok2.DTOs.Task
{
    public class TaskCardDto
    {
        //Карточка задачи для доски
        public int Id { get; set; } //айди задачи
        public string Title { get; set; } = string.Empty; //название, не пустое
        public string Status { get; set; } = string.Empty; //статус
        public DateTime? DueDate { get; set; } //дата дедалйна, если указана

        public string? AssigneeName { get; set; } //кто отвечает за задачу, если указан
    }
}
