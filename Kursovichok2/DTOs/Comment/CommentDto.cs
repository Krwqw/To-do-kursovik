namespace Kursovichok2.DTOs.Comment
{
    public class CommentDto
    {
        public int Id { get; set; } //айди комментораия

        public string Text { get; set; } = string.Empty; //текст коментария, обязательное поле

        public DateTime CreatedAt { get; set; } //дата создания

        public string? AuthorName { get; set; } //автор комментария, ?- потому что поле может быть пустым
    }
}
