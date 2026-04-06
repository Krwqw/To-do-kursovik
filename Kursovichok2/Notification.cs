namespace Kursovichok2
{
    public class Notification
    {
        public int Id { get; set; } //первичный ключ
        public string Text { get; set; } = string.Empty; //текс уведомления, инициализация пустой строкой, чтоб не было ошибок
        public bool IsRead { get; set; } = false; //флажок о том что уведомление прочитано
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //время создания уведомления
        public int UserId { get; set; } //внешний ключ к пользователю
        public int TaskId { get; set; } //внешний ключ к задачам

        //Связи с другими таблицами (один ко многим)
        public User User { get; set; } = null!; //позволяет получить данные пользователя
        public Task Task { get; set; } = null!; //позволяет получить данные из задачи
    }
}
