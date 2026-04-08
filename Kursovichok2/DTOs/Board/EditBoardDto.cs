using System.ComponentModel.DataAnnotations;

namespace Kursovichok2.DTOs.Board
{
    public class EditBoardDto
    {
        //изменение доски
        [StringLength(100)]
        public string? Title { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }
    }
}
