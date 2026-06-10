using System.ComponentModel.DataAnnotations;

namespace PMEHCRM.Models.ViewModels
{
    public class UserLoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
