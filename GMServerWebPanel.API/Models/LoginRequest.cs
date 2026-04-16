using System.ComponentModel.DataAnnotations;

namespace GMServerWebPanel.API.Models
{
    public class LoginRequest
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
