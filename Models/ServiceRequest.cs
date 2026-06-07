using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        public int? ShopId { get; set; }

        public int? MechanicId { get; set; }

        public double CustomerLatitude { get; set; }
        public double CustomerLongitude { get; set; }

        [Required]
        [StringLength(1000)]
        public string SituationDescription { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? DiagnosisNotes { get; set; }

        [StringLength(500)]
        public string? Resolution { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(CustomerId))]
        public ApplicationUser Customer { get; set; } = null!;

        [ForeignKey(nameof(ShopId))]
        public RepairShop? Shop { get; set; }

        [ForeignKey(nameof(MechanicId))]
        public Mechanic? Mechanic { get; set; }

        // Legacy payment (kept for backward compat with seeded data)
        public Payment? Payment { get; set; }

        // New payment models
        public InspectionPayment? InspectionPayment { get; set; }
        public RepairQuotation? RepairQuotation { get; set; }
        public RepairPayment? RepairPayment { get; set; }
        public Invoice? Invoice { get; set; }

        public Feedback? Feedback { get; set; }
    }
}
