using System.Xml.Linq;

namespace Kursovichok2.Models
{
    public class User
    {
        public int Id { get; set; } //первичный ключ
        public string UserName { get; set; } = string.Empty; //логин
        public string Email { get; set; } = string.Empty;//почта
        public string PasswordHash { get; set; } = string.Empty;//хеш пароля
        public string Role { get; set; } = "user"; //роль админ, пользователь, менеджер
        //  = string.Empty - значит защита от того что поле будет незаполненным

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //авто дата при создании пользователя, записывается как ГГГГ-ММ-ДДТчч:мм:ссZ (T и Z - разделение время от даты)

        //Связи с другими таблицами (один ко многим)
        public ICollection<Board> Boards { get; set; } = new List<Board>();
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        //ICollection<> - интерфйс для списков
        //= new List<>() - пустой список, чтоб не было ошибки с нулл
    }
}
