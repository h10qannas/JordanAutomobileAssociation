using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Enter your registered phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
