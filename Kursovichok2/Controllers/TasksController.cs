using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Task;
using Kursovichok2.DTOs.Comment;
using Kursovichok2.Models;

namespace Kursovichok2.Controllers
{
    [Route("api/[controller]")]//базовый путь
    [ApiController]
    [Authorize]//все методы требуют авторизации
    public class TasksController : ControllerBase/* TasksController, который наследуется от ControllerBase (базовый класс для API-контроллеров). 
                                                  * Даёт доступ к методам Ok(), NotFound(), BadRequest()*/
    {
        private readonly AppDbContext _db;

        public TasksController(AppDbContext db) => _db = db;/*Когда ASP.NET создаёт контроллер, он автоматически передаст сюда AppDbContext
                                                             * и сохранит его в поле _db.*/


        [HttpGet]//получить все задачи одной доски
        public async Task<IActionResult> GetTasks([FromQuery] int boardId)
        {
            int userId = GetCurrentUserId();

            var boardExists = await _db.Boards.AnyAsync(b => b.Id == boardId && b.UserId == userId);/*проверяем если доска существует и принадл текущему пользователю
                                                                                                     * результат сохраняем*/
            if (!boardExists) return Forbid();//если доска не существует то доспут запрещен

            var tasks = await _db.Tasks//собирает данные и показывает их в дто
                .Include(t => t.User)
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

        //показать детали одной задачи
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDetailDto>> GetTaskDetails(int id)
        {
            var userId = GetCurrentUserId();//получаем id пользвоателя

            var task = await _db.Tasks//загружает данные и потом выводит их в дто TaskDetailDto
                .Include(t => t.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound();

            var result = new TaskDetailDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                AssigneeName = task.User.UserName,
                Comments = task.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                    AuthorName = c.User.UserName
                }).ToList()
            };

            return Ok(result);
        }

        //метод для фронта (дкбликация)
        [HttpGet("boards/{boardId}/tasks")]
        public async Task<IActionResult> GetTasksForFrontend(int boardId)
        {
            return await GetTasks(boardId);
        }

        //создаем задачу
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)//берем тело запроса
        {
            int userId = GetCurrentUserId();//id пользователя

            var boardExists = await _db.Boards.AnyAsync(b => b.Id == dto.BoardId && b.UserId == userId);//проверка существования доски и принадл пользователю
            if (!boardExists) return Forbid();//отказ

            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)//валидация дедлайна
            {
                return BadRequest(new { message = "Дедлайн не может быть установлен в прошлом" });//ошибка установки дедлайна
            }

            var task = new Ttask//новый объект задачи 
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status ?? "todo",
                DueDate = dto.DueDate,
                BoardId = dto.BoardId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tasks.Add(task);//добавляем задачу в бд таск
            await _db.SaveChangesAsync();

            var result = new TaskCardDto//создаем дто для ответа от сервера
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                DueDate = task.DueDate,
                AssigneeName = User.Identity?.Name
            };

            return CreatedAtAction(nameof(GetTasks), new { boardId = task.BoardId }, result);
        }

        //обновление задачи
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] EditTaskDto dto)//данные для обновления из тела запроса
        {
            int userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);//проверка на принадлежность

            if (task == null) return NotFound();//ошибка

            
            if (dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date)// Проверка дедлайна
            {
                return BadRequest(new { message = "Дедлайн не может быть установлен в прошлом" });
            }

            //проверка изменения статуса задачи
            bool statusChanged = false;
            string oldStatus = task.Status;

            if (!string.IsNullOrEmpty(dto.Status) && task.Status != dto.Status)//если в  дто пришёл статус и он отличается от предыдущего
            {
                task.Status = dto.Status;
                statusChanged = true;
            }

            // Обновляем остальные поля
            if (!string.IsNullOrEmpty(dto.Title)) task.Title = dto.Title;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
            if (dto.Description != null) task.Description = dto.Description;

            await _db.SaveChangesAsync();

            //уведомление о новом статусе
            if (statusChanged)
            {
                string notificationText = dto.Status switch//выбираем текст уведомления в зависимости от нового статус
                {
                    "done" => $"Задача \"{task.Title}\" завершена",
                    "inprogress" => $"Задача \"{task.Title}\" взята в работу",
                    "todo" => $"Задача \"{task.Title}\" возвращена в To Do",
                    _ => $"Изменён статус задачи \"{task.Title}\""
                };

                var notification = new Notification
                {
                    UserId = task.UserId, // Уведомляем владельца задачи
                    Text = notificationText,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    TaskId = task.Id
                };

                _db.Notifications.Add(notification);//сохраняем увед и тд
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        //удаление задачи
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            int userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);//опять принадлежность

            if (task == null) return NotFound();//ошибка
        

            _db.Tasks.Remove(task);//удаляем и сохраняем удаление
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int GetCurrentUserId()//помощник для получения id из токена(id передается черех токен, а не через юрл)
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}