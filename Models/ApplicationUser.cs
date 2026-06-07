using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JAA.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public UserRole Role { get; set; } = UserRole.Customer;

        // Navigation
        public RepairShop? OwnedShop { get; set; }
        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
