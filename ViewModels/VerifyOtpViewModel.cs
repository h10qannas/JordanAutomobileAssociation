using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the 6-digit code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;
    }
}
