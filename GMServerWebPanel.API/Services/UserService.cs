using GMServerWebPanel.API.Data;
using GMServerWebPanel.API.Models;
using GMServerWebPanel.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GMServerWebPanel.API.Services
{
    public class UserService(AppDbContext context, IPasswordHasher passwordHasher) : IUserService
    {
        private readonly AppDbContext _context = context;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        public User? GetUserBy(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                return null;

            User? user = _context.Users.FirstOrDefault(u => u.Login == login);

            if (user == null)
                return null;

            bool isPasswordValid = _passwordHasher.VerifyPassword(password, user.Password);

            if (isPasswordValid)
                return user;
            else
                return null;
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            user.Password = _passwordHasher.HashPassword(user.Password);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }
    }
}
