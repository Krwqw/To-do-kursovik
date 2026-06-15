using System.Xml.Linq;

namespace Kursovichok2.Models
{
    public class Ttask
    {
        public int Id { get; set; } //первичный ключ
        public string Title { get; set; } = string.Empty; //название задачи, защита от нулл
        public string? Description { get; set; } //описание задачи, может быть пустым
        public string Status { get; set; } = "todo"; //статус задачи (не распределена, в процессе, готово) 
        public DateTime? DueDate { get; set; } //дедлайн, может быть не указан
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //дата создания новой задачи
        public int BoardId { get; set; } //внешний ключ к доскам
        public int UserId { get; set; } //внешний ключ к пользователям

        //Связи с другими таблицами (один ко многим)
        public Board Board { get; set; } = null!; //получим доску, к которой относится задача
        public User User { get; set; } = null!; //получим пользователя, к которому относится задача
        public ICollection<Comment> Comments { get; set; } = new List<Comment>(); //сразу пустой список для новой задачи
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>(); //сразу пустой список для уведомлений
    }
}

