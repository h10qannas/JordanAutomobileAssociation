using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class Mechanic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShopId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ProfileImageUrl { get; set; }

        public int? YearsOfExperience { get; set; }

        public MechanicStatus Status { get; set; } = MechanicStatus.Pending;

        public bool IsAvailable { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ShopId))]
        public RepairShop Shop { get; set; } = null!;

        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<RepairQuotation> Quotations { get; set; } = new List<RepairQuotation>();
    }
}
