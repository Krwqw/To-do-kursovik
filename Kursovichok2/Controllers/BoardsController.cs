using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Board;
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BoardsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BoardsController(AppDbContext db) => _db = db;

        // Получить мои доски
        [HttpGet]
        public async Task<IActionResult> GetMyBoards()
        {
            int userId = GetUserId();
            var boards = await _db.Boards
                .Where(b => b.UserId == userId)
                .Select(b => new BoardCardDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    CreatedAt = b.CreatedAt,
                    OwnerId = b.UserId
                })
                .ToListAsync();

            return Ok(boards);
        }

        // Создать доску
        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] CreateBoardDto dto)
        {
            int userId = GetUserId();
            var board = new Board
            {
                Title = dto.Title,
                Description = dto.Description,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Boards.Add(board);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyBoards), new { id = board.Id }, board);
        }

        // Удалить доску
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            int userId = GetUserId();
            var board = await _db.Boards.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null) return NotFound("Доска не найдена или нет прав");

            _db.Boards.Remove(board);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Вспомогательный метод: взять ID из токена
        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}