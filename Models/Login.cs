using System.ComponentModel.DataAnnotations;

namespace PMEHCRM.Models
{
    public class Login
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } = "User"; // Default role is "User"
    }
}
