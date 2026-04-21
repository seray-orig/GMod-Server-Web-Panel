using GMServerWebPanel.API.Models;

namespace GMServerWebPanel.API.Services.Interfaces
{
    public interface ITokenServise
    {
        string GenerateToken(User user, bool rememberMe);
    }
}
