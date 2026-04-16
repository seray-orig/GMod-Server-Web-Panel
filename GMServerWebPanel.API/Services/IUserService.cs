using GMServerWebPanel.API.Models;

namespace GMServerWebPanel.API.Services
{
    public interface IUserService
    {
        public User? GetUserBy(string login, string password);
    }
}
