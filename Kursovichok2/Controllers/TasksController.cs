using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Comment;
using Kursovichok2.DTOs.Task;
using Kursovichok2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTask = Kursovichok2.Models.Task;

namespace Kursovichok2.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TasksController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DetalTaskDto>> GetTask(int id)
        {
            var task = await FindUserTask(id);
            return task is null ? NotFound(new { message = "Задача не найдена." }) : ToDetailDto(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskCardDto>> CreateTask(CreateTaskDto dto)
        {
            var userId = GetUserId();
            var boardExists = await _db.Boards.AnyAsync(b => b.Id == dto.BoardId && b.UserId == userId);
            if (!boardExists)
            {
                return NotFound(new { message = "Доска не найдена." });
            }

            var task = new ProjectTask
            {
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Status = NormalizeStatus(dto.Status),
                DueDate = dto.DueDate,
                BoardId = dto.BoardId,
                UserId = userId
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, new TaskCardDto
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                DueDate = task.DueDate,
                AssigneeName = User.Identity?.Name
            });
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<DetalTaskDto>> EditTask(int id, EditTaskDto dto)
        {
            var task = await FindUserTask(id);
            if (task is null)
            {
                return NotFound(new { message = "Задача не найдена." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                task.Title = dto.Title.Trim();
            }

            task.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                task.Status = NormalizeStatus(dto.Status);
            }

            task.DueDate = dto.DueDate;
            AddNotification(task, $"Задача \"{task.Title}\" обновлена.");
            await _db.SaveChangesAsync();

            return ToDetailDto(task);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await FindUserTask(id);
            if (task is null)
            {
                return NotFound(new { message = "Задача не найдена." });
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult<VievCommDto>> AddComment(int id, CreateCommDto dto)
        {
            var task = await FindUserTask(id);
            if (task is null)
            {
                return NotFound(new { message = "Задача не найдена." });
            }

            var comment = new Comment
            {
                Text = dto.Text.Trim(),
                TaskId = id,
                UserId = GetUserId()
            };

            _db.Comments.Add(comment);
            AddNotification(task, $"Добавлен комментарий к задаче \"{task.Title}\".");
            await _db.SaveChangesAsync();

            return new VievCommDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                AuthorName = User.Identity?.Name
            };
        }

        private async Task<ProjectTask?> FindUserTask(int taskId)
        {
            var userId = GetUserId();
            return await _db.Tasks
                .Include(t => t.Board)
                .Include(t => t.User)
                .Include(t => t.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.Board.UserId == userId);
        }

        private void AddNotification(ProjectTask task, string text)
        {
            _db.Notifications.Add(new Notification
            {
                Text = text,
                TaskId = task.Id,
                UserId = GetUserId()
            });
        }

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        private static string NormalizeStatus(string? status)
        {
            return status?.Trim().ToLowerInvariant() switch
            {
                "inprogress" => "inprogress",
                "done" => "done",
                _ => "todo"
            };
        }

        private static DetalTaskDto ToDetailDto(ProjectTask task)
        {
            return new DetalTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                Comments = task.Comments
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new VievCommDto
                    {
                        Id = c.Id,
                        Text = c.Text,
                        CreatedAt = c.CreatedAt,
                        AuthorName = c.User.UserName
                    })
                    .ToList()
            };
        }
    }
}
