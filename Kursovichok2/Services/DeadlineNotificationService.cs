using Kursovichok2.Data;
using Kursovichok2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Kursovichok2.Services
{
    public class DeadlineNotificationService : BackgroundService//фоновый сервис уведомлений для проверки дедлайнов задач, наследуется от BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DeadlineNotificationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    
                    using (var scope = _scopeFactory.CreateScope())//доступ для работы с БД в фоне (подключение HTTP-запроса от пользователя (это называется "scoped"))
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var today = DateTime.UtcNow.Date; // Дата сегодня

                        
                        var notifiedTaskIds = await context.Notifications//защита от спама, чтоб дубликаты не перегружали сайт
                            .Where(n => n.CreatedAt.Date == today && n.Text.Contains("дедлайн"))
                            .Select(n => n.TaskId)
                            .ToListAsync();

                        var overdueTasks = await context.Tasks//поиск просроченных задач которые не находятся в колонке done и которые еще не получили уведомление сегодня

                            .Include(t => t.User)
                            .Where(t => t.Status != "done"
                                     && t.DueDate.HasValue
                                     && t.DueDate.Value.Date <= today
                                     && !notifiedTaskIds.Contains(t.Id))
                            .ToListAsync();

                        foreach (var task in overdueTasks)//создание уведомлений для полльзователя
                        {
                            var notification = new Notification
                            {
                                UserId = task.UserId, // Уведомление владельцу задачи
                                Text = $"️ Наступил дедлайн задачи \"{task.Title}\"!",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow,
                                TaskId = task.Id
                            };

                            context.Notifications.Add(notification);
                        }

                        await context.SaveChangesAsync();//сохранение уведов в бд
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Ошибка в DeadlineService: {ex.Message}");//если бд перестанет работать, то сервер останется рабоатть
                }
                    
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);//после каждой проверки просроченных задач слудет сон в 24 ч
            }
        }
    }
}