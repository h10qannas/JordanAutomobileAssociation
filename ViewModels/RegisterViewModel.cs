using System.ComponentModel.DataAnnotations;
using JAA.Models;

namespace JAA.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Enter your full name")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your phone number")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Create a password (min 6 characters)")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Customer;

        // Shop Owner extended fields — only required when Role == ShopOwner
        [Display(Name = "Shop Name")]
        public string? ShopName { get; set; }

        [Display(Name = "City")]
        public string? ShopCity { get; set; }

        [Phone]
        [Display(Name = "Shop Phone")]
        public string? ShopPhone { get; set; }

        [Display(Name = "Shop Address")]
        public string? ShopAddress { get; set; }
    }
}
