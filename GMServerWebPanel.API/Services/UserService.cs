using GMServerWebPanel.API.Models;

namespace GMServerWebPanel.API.Services
{
    public class UserService : IUserService
    {
        private readonly List<User> _users =
        [
            new User { Login = "TestUser", Password = "TestUser" }
        ];

        public User? GetUserBy(string login, string password)
        {
            return _users.FirstOrDefault(u =>
                u.Login == login && u.Password == password);
        }
    }
}
