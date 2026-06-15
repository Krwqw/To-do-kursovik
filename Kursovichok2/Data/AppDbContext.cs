using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Kursovichok2.Models;

namespace Kursovichok2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }  //принимает настройки подключения, получает необходимые ему зависимости извне

        //представление таблиц в базе данных, через них выполняются все операции в будущем

        public DbSet<User> Users { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<Kursovichok2.Models.Task> Tasks { get; set; } //конфликт поэтому полное название класса task
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //метод который настраивает как классы превращаются в таблицы
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id); //первичный ключ
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(50); //имя пользователя, макс 50 символов, 
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100); //
                entity.HasIndex(e => e.Email).IsUnique(); //хэш индекс, уникальный, чтоб не было повторений пользователей
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255); //
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20); //
            });
            //*IsRequired() - не даст сохранить, если нет имени или почты

            //конфигурация досок (набор настроек для досок)
            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Boards)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //конфигурация задач
            modelBuilder.Entity<Kursovichok2.Models.Task>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

                entity.HasOne(e => e.Board)
                      .WithMany(b => b.Tasks)
                      .HasForeignKey(e => e.BoardId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Tasks)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //конфигурация комментариев
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();

                entity.HasOne(e => e.Task)
                      .WithMany(t => t.Comments)
                      .HasForeignKey(e => e.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //конфигурация уведомлений
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Task)
                      .WithMany(t => t.Notifications)
                      .HasForeignKey(e => e.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
