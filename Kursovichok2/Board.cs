namespace Kursovichok2
{
    public class Board
    {
        public int Id { get; set; } //первичный ключ
        public string Title { get; set; } = string.Empty; //название доски и защита от нулл, чтоб не было ошибки
        public string? Description { get; set; } //комментарий к доске. ? подразумевает что может быть нулл
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //дата создания доски
        public int UserId { get; set; } //внешний ключ к доске Users

        //Связи с другими таблицами (один ко многим)
        public User User { get; set; } = null!; //позволяет получить сощдателя доски. обязательно будет выполнено
        public ICollection<Task> Tasks { get; set; } = new List<Task>(); //полуячение всех задач на доске
    }
}
