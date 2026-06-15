using System.Security.Claims;
using Kursovichok2.Data;
using Kursovichok2.DTOs.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kursovichok2.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<VievNotifDto>>> GetNotifications()
        {
            var userId = GetUserId();
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new VievNotifDto
                {
                    Id = n.Id,
                    Text = n.Text,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TaskId = n.TaskId
                })
                .ToListAsync();
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == GetUserId());
            if (notification is null)
            {
                return NotFound(new { message = "Уведомление не найдено." });
            }

            notification.IsRead = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }
}
