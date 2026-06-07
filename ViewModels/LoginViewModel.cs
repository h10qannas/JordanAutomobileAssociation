using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class LoginViewModel
    {
        // Accepts phone number (for new users) or email (for seeded/legacy users)
        [Required(ErrorMessage = "Enter your phone number")]
        [Display(Name = "Phone Number")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
