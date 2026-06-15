using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Board;
using Kursovichok2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kursovichok2.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/boards")]
    public class BoardsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BoardsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<BoardCardDto>>> GetBoards()
        {
            var userId = GetUserId();
            return await _db.Boards
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => ToDto(b))
                .ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BoardCardDto>> GetBoard(int id)
        {
            var board = await FindUserBoard(id);
            return board is null ? NotFound(new { message = "Доска не найдена." }) : ToDto(board);
        }

        [HttpPost]
        public async Task<ActionResult<BoardCardDto>> CreateBoard(CreateBoardDto dto)
        {
            var board = new Board
            {
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                UserId = GetUserId()
            };

            _db.Boards.Add(board);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, ToDto(board));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<BoardCardDto>> EditBoard(int id, EditBoardDto dto)
        {
            var board = await FindUserBoard(id);
            if (board is null)
            {
                return NotFound(new { message = "Доска не найдена." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                board.Title = dto.Title.Trim();
            }

            board.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            await _db.SaveChangesAsync();

            return ToDto(board);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var board = await FindUserBoard(id);
            if (board is null)
            {
                return NotFound(new { message = "Доска не найдена." });
            }

            _db.Boards.Remove(board);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{boardId:int}/tasks")]
        public async Task<ActionResult<List<DTOs.Task.TaskCardDto>>> GetBoardTasks(int boardId)
        {
            if (await FindUserBoard(boardId) is null)
            {
                return NotFound(new { message = "Доска не найдена." });
            }

            return await _db.Tasks
                .Where(t => t.BoardId == boardId)
                .Include(t => t.User)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new DTOs.Task.TaskCardDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    AssigneeName = t.User.UserName
                })
                .ToListAsync();
        }

        private async Task<Board?> FindUserBoard(int boardId)
        {
            var userId = GetUserId();
            return await _db.Boards.FirstOrDefaultAsync(b => b.Id == boardId && b.UserId == userId);
        }

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        private static BoardCardDto ToDto(Board board)
        {
            return new BoardCardDto
            {
                Id = board.Id,
                Title = board.Title,
                Description = board.Description,
                CreatedAt = board.CreatedAt,
                OwnerId = board.UserId
            };
        }
    }
}
