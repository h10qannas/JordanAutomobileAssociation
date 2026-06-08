using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class Testimonial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        public int ServiceRequestId { get; set; }

        public int ShopId { get; set; }

        public int? MechanicId { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;

        public bool IsFeatured { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedById { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // Navigation
        [ForeignKey(nameof(CustomerId))]
        public ApplicationUser Customer { get; set; } = null!;

        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        [ForeignKey(nameof(ShopId))]
        public RepairShop Shop { get; set; } = null!;

        [ForeignKey(nameof(MechanicId))]
        public Mechanic? Mechanic { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public ApplicationUser? ApprovedBy { get; set; }
    }
}
