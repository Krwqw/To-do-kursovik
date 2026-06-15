using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Task;
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

        //  ОСНОВНОЙ МЕТОД: Получение задач по ID доски (через query параметр ?boardId=...)
        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] int boardId)
        {
            int userId = GetCurrentUserId();

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
                    AssigneeName = t.User.UserName
                })
                .ToListAsync();

            return Ok(tasks);
        }

        // 🔹 ПЕРЕХОДНИК ДЛЯ ФРОНТЕНДА: Принимает запрос /api/boards/{boardId}/tasks
        // Это решит твою ошибку 404 Not Found
        [HttpGet("boards/{boardId}/tasks")]
        public async Task<IActionResult> GetTasksForFrontend(int boardId)
        {
            // Просто вызываем основной метод выше
            return await GetTasks(boardId);
        }

        // 🔹 Создать задачу
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            int userId = GetCurrentUserId();

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

            return CreatedAtAction(nameof(GetTasks), new { boardId = task.BoardId }, task);
        }

        // 🔹 Обновить задачу
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] EditTaskDto dto)
        {
            int userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Title)) task.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Status)) task.Status = dto.Status;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
            if (dto.Description != null) task.Description = dto.Description;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 Удалить задачу
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            int userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Вспомогательный метод для получения ID пользователя
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}