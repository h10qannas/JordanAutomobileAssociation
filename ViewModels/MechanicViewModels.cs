using System.ComponentModel.DataAnnotations;
using JAA.Models;

namespace JAA.ViewModels
{
    public class AddMechanicViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "National ID is required")]
        [StringLength(20)]
        [Display(Name = "National ID Number")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Years of Experience")]
        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfileImage { get; set; }
    }

    public class EditMechanicViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Years of Experience")]
        [Range(0, 60)]
        public int? YearsOfExperience { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfileImage { get; set; }

        public string? ExistingProfileImageUrl { get; set; }
        public string? NationalId { get; set; }
        public MechanicStatus Status { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class MechanicListViewModel
    {
        public List<Mechanic> Mechanics { get; set; } = new();
        public int ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class MechanicDetailViewModel
    {
        public Mechanic Mechanic { get; set; } = null!;
        public double AverageRating { get; set; }
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
    }
}
