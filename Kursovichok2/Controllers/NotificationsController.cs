using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kursovichok2.Controllers
{
    [ApiController]
    [Authorize]//все методы этого контроллера требуют JWT-токен
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]//получить все уведомления
        public async Task<ActionResult<List<VievNotifDto>>> GetNotifications()
        {
            var userId = GetUserId();//id текущего пользователя из токена
            return await _db.Notifications
                .Where(n => n.UserId == userId)//поиск в базе только его уведомления
                .OrderByDescending(n => n.CreatedAt)//сортировка сначала самые новые
                .Select(n => new VievNotifDto //отображение данных только в виде нужных полей
                {
                    Id = n.Id,
                    Text = n.Text,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TaskId = n.TaskId
                })
                .ToListAsync();
        }

        [HttpPut("{id:int}/read")]//отметка одного уведомления как прочитанное
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == GetUserId());/*поиск уведомления по id
                                                                                                                        * и проверка о принадлежности 
                                                                                                                        * к текущему пользователю*/

            if (notification is null)
            {
                return NotFound(new { message = "Уведомление не найдено." });
            }

            notification.IsRead = true;//ставит отметку о прочтении
            await _db.SaveChangesAsync();//сохраняет изменения(т.е. что уведомление прочитано)
            return NoContent();//возвращает 204 NoContent (всё выполнено, но данных в ответе нет)
        }

        [HttpPut("read-all")]//отметка всех уведомлений как прочитанное
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();//поиск всех непрочитанных
            foreach (var notification in notifications)
            {
                notification.IsRead = true;//делает все уведомления прочитанными
            }

            await _db.SaveChangesAsync();//сохраняет статус
            return NoContent();
        }

        private int GetUserId()//вспомогательный метод
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;/*берет id пользователя из токена (из "клейма" NameIdentifier). 
                                                             *если не удалось достать id не удалось достать — 
                                                             *то вернёт 0 */
        }
    }
}
