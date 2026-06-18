namespace Kursovichok2.Models
{
    public class Comment
    {
        public int Id { get; set; } //айди комментария для бд
        public string Text { get; set; } = string.Empty; //текст комментария и защита от незаполнености
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //дата и время создания комментария
        public int TaskId { get; set; } //внешний ключ к задачам
        public int UserId { get; set; } //внешний ключ к пользователям

        //связи с другими таблицами (многие к одному)
        public Ttask Task { get; set; } = null!; //позволяет получить объект задачи из комментария
        public User User { get; set; } = null!; //позволяет получить данные автора комментария
    } 
}
