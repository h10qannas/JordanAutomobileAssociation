using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class ShopRegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Your Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Your Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required (min 6 characters)")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(200)]
        [Display(Name = "Shop Name")]
        public string ShopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Owner name is required")]
        [StringLength(100)]
        [Display(Name = "Owner Full Name")]
        public string OwnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shop phone number is required")]
        [Phone]
        [Display(Name = "Shop Phone Number")]
        public string ShopPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string ShopCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shop address is required")]
        [StringLength(300)]
        [Display(Name = "Shop Address")]
        public string ShopAddress { get; set; } = string.Empty;

        [Display(Name = "Business Certificate")]
        public IFormFile? BusinessCertificate { get; set; }
    }
}
