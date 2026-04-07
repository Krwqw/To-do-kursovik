namespace Kursovichok2.DTOs.Board
{
    public class BoardCardDto
    {
        //карточка доски
        public int Id { get; set; } //первичный ключ доски

        public string Title { get; set; } = string.Empty; //название

        public string? Description { get; set; } //описание, не обязатльео

        public DateTime CreatedAt { get; set; } //дата создания

        public int OwnerId { get; set; } //только айди владельца

    }
}
