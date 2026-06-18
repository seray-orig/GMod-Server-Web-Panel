using System.ComponentModel.DataAnnotations;

namespace GMServerWebPanel.API.Models
{
    public class SendCommand
    {
        [Required]
        public required string Command
        {
            get;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Length <= 256)
                    field = value;
            }
        }
    }
}
