using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GMServerWebPanel.API.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, IPasswordHasher passwordHasher) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasKey(u => u.Login);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Login = "TestLogin",
                    Password = passwordHasher.HashPassword("TestPassword")
                }
            );
        }
    }
}
