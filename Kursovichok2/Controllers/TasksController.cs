using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Task; // Твои DTO
using Kursovichok2.DTOs.Comment; // Для комментариев внутри задачи
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TasksController(AppDbContext db) => _db = db;

        // Получить задачи конкретной доски
        [HttpGet]
        public async Task<IActionResult> GetTasksByBoard([FromQuery] int boardId)
        {
            int userId = GetUserId();

            // Проверка: эта доска принадлежит мне?
            var boardExists = await _db.Boards.AnyAsync(b => b.Id == boardId && b.UserId == userId);
            if (!boardExists) return Forbid();

            var tasks = await _db.Tasks
                .Where(t => t.BoardId == boardId)
                .Select(t => new TaskCardDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    AssigneeName = t.User.UserName // Берем имя исполнителя
                })
                .ToListAsync();

            return Ok(tasks);
        }

        // Создать задачу
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            int userId = GetUserId();

            // Проверка прав на доску
            var boardExists = await _db.Boards.AnyAsync(b => b.Id == dto.BoardId && b.UserId == userId);
            if (!boardExists) return Forbid();

            var task = new Ttask
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status ?? "todo",
                DueDate = dto.DueDate,
                BoardId = dto.BoardId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTasksByBoard), new { boardId = task.BoardId }, task);
        }

        // Обновить задачу (или просто сменить статус)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] EditTaskDto dto)
        {
            int userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound();

            // Обновляем только то, что пришло
            if (!string.IsNullOrEmpty(dto.Title)) task.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Status)) task.Status = dto.Status;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
            // Описание можно обновлять аналогично

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Удалить задачу
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            int userId = GetUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}