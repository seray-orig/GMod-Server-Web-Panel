using System.ComponentModel.DataAnnotations;

namespace GMServerWebPanel.API.Models
{
    public class LoginRequest
    {
        [Required]
        public required string Login
        {
            get;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length <= 36)
                    field = value;
            }
        }

        [Required]
        public required string Password
        {
            get;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length <= 64)
                    field = value;
            }
        }

        public bool RememberMe { get; set; }
    }
}
