using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Comment;
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]  // Базовый URL: api/comments
    [ApiController]              // Авто-валидация и обработка ошибок
    [Authorize]                  // Все методы требуют JWT-токен
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;  // Подключение к базе данных
        }

        

        [HttpPost]//создание комментария
        public async Task<ActionResult<CommentDto>> AddComment([FromBody] CreateCommDto dto)
        {
            // Проверка валидности данных (обязательные поля заполнены)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Достаём ID пользователя из токена (безопасно, нельзя подделать)
            var userId = GetCurrentUserId();

            // Проверяем: задача существует И принадлежит текущему пользователю
            var task = await _context.Tasks
                .Include(t => t.Board)  // Подгружаем доску задачи
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            // Если задача не найдена или доска чужая — доступ запрещён
            if (task == null || task.Board.UserId != userId)
                return Forbid();

            // Создаём новый комментарий
            var comment = new Comment
            {
                Text = dto.Text,           // Текст комментария
                TaskId = dto.TaskId,       // К какой задаче
                UserId = userId,           // Кто написал
                CreatedAt = DateTime.UtcNow // Время создания (UTC)
            };

            _context.Comments.Add(comment);  // Добавляем в БД (пока в памяти)
            await _context.SaveChangesAsync(); // Сохраняем в базу

            // Возвращаем 201 Created с данными нового комментария
            return CreatedAtAction(nameof(GetComments), new { taskId = comment.TaskId }, new CommentDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                AuthorName = User.Identity?.Name  // Имя автора из токена
            });
        }

        

        [HttpPost("tasks/{taskId}/comments")]//переход для фронта красиво чтоб все быглядело
        public async Task<ActionResult<CommentDto>> AddCommentForFrontend(int taskId, [FromBody] CreateCommDto dto)
        {
            // Защита: ID в URL и в теле должны совпадать
            if (dto.TaskId != taskId)
                return BadRequest("Несоответствие ID задачи");

            // Вызываем основной метод
            return await AddComment(dto);
        }


        [HttpGet("task/{taskId}")]//все комментарии у задачи
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int taskId)
        {
            var userId = GetCurrentUserId();  // ID из токена

            // Проверяем: задача существует И принадлежит пользователю
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Id == taskId && t.Board.UserId == userId);

            // Если задача не найдена или чужая — 404
            if (!taskExists)
                return NotFound(new { message = "Задача не найдена или доступ запрещен" });

            // Загружаем комментарии, сортируем по дате (старые → новые)
            var comments = await _context.Comments
                .Where(c => c.TaskId == taskId)  // Только для этой задачи
                .Include(c => c.User)            // Подгружаем автора
                .OrderBy(c => c.CreatedAt)       // Сортировка по времени
                .Select(c => new CommentDto      // Превращаем в DTO
                {
                    Id = c.Id,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                    AuthorName = c.User.UserName  // Имя автора
                })
                .ToListAsync();  // Выполняем SQL-запрос

            return Ok(comments);  // 200 OK с массивом комментариев
        }

        [HttpDelete("{id}")]//удаление комментария
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = GetCurrentUserId();  // ID из токена

            // Ищем комментарий: ID совпадает И принадлежит пользователю
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            // Если не найден или чужой — 404
            if (comment == null)
                return NotFound(new { message = "Комментарий не найден или вы не имеете прав на его удаление" });

            _context.Comments.Remove(comment);  // Помечаем на удаление
            await _context.SaveChangesAsync();  // Удаляем из БД

            return NoContent();  // 204 NoContent (успех без данных)
        }

       
        private int GetCurrentUserId() //метод помогает достать ID пользователя из JWT-токена
        {
            // Берём клейм NameIdentifier из токена и превращаем в число
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}