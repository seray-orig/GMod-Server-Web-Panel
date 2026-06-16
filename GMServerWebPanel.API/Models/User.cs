using System.ComponentModel.DataAnnotations;

namespace GMServerWebPanel.API.Models
{
    public class User
    {
        [Key]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }

        public static User GenerateRandomUser()
        {
            return new User
            {
                Login = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString()
            };
        }
    }
}
