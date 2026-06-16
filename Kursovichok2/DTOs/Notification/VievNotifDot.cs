namespace Kursovichok2.DTOs.Notification
{
    public class VievNotifDot
    {
        //просмотр уведомления
        public int Id { get; set; } //айди уведомления

        public string Text { get; set; } = string.Empty; //текст уведомления

        public bool IsRead { get; set; } //флажок по умолчанию - не прочитано

        public DateTime CreatedAt { get; set; } //дата создания

        public int TaskId { get; set; } //айли задачи к которой есть уведомление
    }
}
