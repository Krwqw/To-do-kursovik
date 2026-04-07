using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Board
{
    public class CreateDto
    {
        //создание доски
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty; //название доски

        [StringLength(500)]
        public string? Description { get; set; } //описание, не обязательно
    }
}
