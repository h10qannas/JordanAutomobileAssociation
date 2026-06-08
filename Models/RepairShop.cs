using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class RepairShop
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ShopName { get; set; } = string.Empty;

        [StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; } = string.Empty;

        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }

        [StringLength(500)]
        public string? BusinessCertificateUrl { get; set; }

        public bool IsVerified { get; set; } = false;

        public ShopStatus ShopStatus { get; set; } = ShopStatus.Pending;

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(OwnerId))]
        public ApplicationUser Owner { get; set; } = null!;

        public ICollection<Mechanic> Mechanics { get; set; } = new List<Mechanic>();
        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
