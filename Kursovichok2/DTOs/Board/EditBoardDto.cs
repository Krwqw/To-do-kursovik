using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Board
{
    public class EditBoardDto
    {
        //изменение доски
        [StringLength(100, ErrorMessage = "Максимум 100 символов")]
        public string? Title { get; set; }

        [StringLength(500, ErrorMessage = "Максимум 500 символов")]
        public string? Description { get; set; }
    }
}
