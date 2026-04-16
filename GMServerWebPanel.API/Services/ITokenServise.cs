using GMServerWebPanel.API.Models;

namespace GMServerWebPanel.API.Services
{
    public interface ITokenServise
    {
        string GenerateToken(User user, bool rememberMe);
    }
}
