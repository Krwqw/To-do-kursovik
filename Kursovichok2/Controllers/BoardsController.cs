using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Board; // Убедись, что у тебя есть такой namespace для DTO
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Требует авторизации для всех методов
    public class BoardsController : ControllerBase
    {
        //объявляем поле для контекста БД
        private readonly AppDbContext _context;

        //Конструктор, который получает контекст через извне, чтоб не создавать их самост
        public BoardsController(AppDbContext context)
        {
            _context = context;
        }

        //Получить все доски текущего пользователя
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

        //Получить одну доску по ID
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

            if (board == null)
            {
                return NotFound();
            }

            return Ok(board);
        }

        //Создать новую доску
        [HttpPost]
        public async Task<ActionResult<BoardCardDto>> CreateBoard([FromBody] CreateBoardDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            //Возвращаем 201 Created и ссылку на созданный ресурс
            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, result);
        }

        //Обновить доску (частичное обновление)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBoard(int id, [FromBody] EditBoardDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();

            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null)
                return NotFound();

            // Обновляем только если поля переданы
            if (dto.Title != null)
                board.Title = dto.Title;

            if (dto.Description != null)
                board.Description = dto.Description;

            await _context.SaveChangesAsync();

            return NoContent(); // Возвращаем 204 No Content
        }

        //Удалить доску
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var userId = GetCurrentUserId();

            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (board == null)
                return NotFound();

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // Метод для получения ID текущего пользователя из JWT токена
        private int GetCurrentUserId()
        {
            // Ищем claim с именем NameIdentifier (стандарт для ID пользователя в JWT)
            /*Claim (утверждение или заявленное свойство) — это элемент системы безопасности,
             * который содержит информацию о пользователе (например, его email, роль или возраст) в виде пары «ключ-значение»*/


            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            return int.Parse(userIdClaim);
        }
    }
}