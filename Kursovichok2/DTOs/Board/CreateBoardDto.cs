using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Board
{
    public class CreateBoardDto
    {
        //создание доски
        [Required(ErrorMessage = "Название доски обязательно")]
        [StringLength(100, ErrorMessage = "Максимум 100 символов")]
        public string Title { get; set; } = string.Empty; //название доски

        [StringLength(500)]
        public string? Description { get; set; } //описание, не обязательно
    }
}
