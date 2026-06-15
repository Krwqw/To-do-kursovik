using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Board; // ⚠️ Если VS ругается, замени на Kursovichok2.DTOs
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 Все операции с досками только для авторизованных
    public class BoardsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BoardsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Получить все доски текущего пользователя
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BoardCardDto>>> GetBoards()
        {
            var userId = GetCurrentUserId();

            var boards = await _context.Boards
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

        // 🔹 Получить одну доску по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<BoardCardDto>> GetBoard(int id)
        {
            var userId = GetCurrentUserId();

            var board = await _context.Boards
                .Where(b => b.Id == id && b.UserId == userId)
                .Select(b => new BoardCardDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    CreatedAt = b.CreatedAt,
                    OwnerId = b.UserId
                })
                .FirstOrDefaultAsync();

            if (board == null) return NotFound();
            return Ok(board);
        }

        // 🔹 Создать новую доску
        [HttpPost]
        public async Task<ActionResult<BoardCardDto>> CreateBoard([FromBody] CreateBoardDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var board = new Board
            {
                Title = dto.Title,
                Description = dto.Description,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync();

            var result = new BoardCardDto
            {
                Id = board.Id,
                Title = board.Title,
                Description = board.Description,
                CreatedAt = board.CreatedAt,
                OwnerId = board.UserId
            };

            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, result);
        }

        // 🔹 Обновить доску (частичное обновление)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBoard(int id, [FromBody] EditBoardDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null) return NotFound();

            if (dto.Title != null) board.Title = dto.Title;
            if (dto.Description != null) board.Description = dto.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 Удалить доску
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var userId = GetCurrentUserId();
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null) return NotFound();

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 Вспомогательный метод: получить ID текущего пользователя из JWT
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // 🔧 ВРЕМЕННО для тестов (без авторизации)
            //return 1; // Используем ID первого пользователя из БД
        }
    }
}