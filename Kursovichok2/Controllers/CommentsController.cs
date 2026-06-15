using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Comment;
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        //  ОСНОВНОЙ МЕТОД: POST /api/comments
        [HttpPost]
        public async Task<ActionResult<CommentDto>> AddComment([FromBody] CreateCommDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();

            // Проверка: задача должна существовать и принадлежать пользователю
            var task = await _context.Tasks
                .Include(t => t.Board)
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null || task.Board.UserId != userId)
                return Forbid();

            var comment = new Comment
            {
                Text = dto.Text,
                TaskId = dto.TaskId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);

            // Создаем уведомление для владельца задачи, если комментирует не он
            if (task.UserId != userId)
            {
                var notification = new Notification
                {
                    Text = $"Новый комментарий к задаче \"{task.Title}\"",
                    UserId = task.UserId,
                    TaskId = task.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComments), new { taskId = comment.TaskId }, new CommentDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                AuthorName = User.Identity?.Name
            });
        }

        // 🔹 ПЕРЕХОДНИК ДЛЯ ФРОНТЕНДА: POST /api/tasks/{taskId}/comments
        // Именно этот метод решит твою ошибку 404!
        [HttpPost("tasks/{taskId}/comments")]
        public async Task<ActionResult<CommentDto>> AddCommentForFrontend(int taskId, [FromBody] CreateCommDto dto)
        {
            if (dto.TaskId != taskId)
                return BadRequest("Несоответствие ID задачи");

            return await AddComment(dto);
        }

        // 🔹 Получить комментарии к задаче: GET /api/comments/task/{taskId}
        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int taskId)
        {
            var userId = GetCurrentUserId();

            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == taskId && t.Board.UserId == userId);

            if (!taskExists)
                return NotFound(new { message = "Задача не найдена или доступ запрещен" });

            var comments = await _context.Comments
                .Where(c => c.TaskId == taskId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
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

        // 🔹 Удалить комментарий: DELETE /api/comments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = GetCurrentUserId();

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (comment == null)
                return NotFound(new { message = "Комментарий не найден или вы не имеете прав на его удаление" });

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Вспомогательный метод
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}