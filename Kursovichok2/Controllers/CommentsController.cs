using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Comment; // Убедись, что у тебя есть этот namespace
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Требуется авторизация для всех действий
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        //  1. Получить все комментарии конкретной задачи
        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int taskId)
        {
            var userId = GetCurrentUserId();

            // Проверка: задача должна существовать и принадлежать пользователю (или его доске)
            // Для простоты проверяем, что задача существует и привязана к доске пользователя
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == taskId && t.Board.UserId == userId);

            if (!taskExists)
                return NotFound(new { message = "Задача не найдена или доступ запрещен" });

            var comments = await _context.Comments
                .Where(c => c.TaskId == taskId)
                .Include(c => c.User) // Подгружаем автора комментария
                .OrderBy(c => c.CreatedAt) // Сортируем по времени создания
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                    AuthorName = c.User.UserName
                })
                .ToListAsync();

            return Ok(comments);
        }

        // 🔹 2. Добавить новый комментарий к задаче
        [HttpPost]
        public async Task<ActionResult<CommentDto>> AddComment([FromBody] CreateCommDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();

            // Проверка прав: можно комментировать только задачи из своих досок
            var task = await _context.Tasks
                .Include(t => t.Board)
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null || task.Board.UserId != userId)
                return Forbid(); // Или NotFound, чтобы не раскрывать наличие чужих задач

            var comment = new Comment
            {
                Text = dto.Text,
                TaskId = dto.TaskId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);

            // 🔔 АВТОМАТИЧЕСКОЕ СОЗДАНИЕ УВЕДОМЛЕНИЯ
            // Если комментирует не владелец задачи, создаем уведомление для владельца
            if (task.UserId != userId)
            {
                var notification = new Notification
                {
                    Text = $"Новый комментарий к задаче \"{task.Title}\" от {User.Identity?.Name}",
                    UserId = task.UserId, // Уведомление получает владелец задачи
                    TaskId = task.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            // Возвращаем созданный комментарий с именем автора
            return CreatedAtAction(nameof(GetComments), new { taskId = comment.TaskId }, new CommentDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                AuthorName = User.Identity?.Name
            });
        }

        // 🔹 3. Удалить комментарий
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = GetCurrentUserId();

            // Найти комментарий и проверить, что он принадлежит текущему пользователю
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (comment == null)
                return NotFound(new { message = "Комментарий не найден или вы не имеете прав на его удаление" });

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Вспомогательный метод для получения ID текущего пользователя из JWT
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}