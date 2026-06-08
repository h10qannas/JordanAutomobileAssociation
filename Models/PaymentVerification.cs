using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class PaymentVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MechanicReportedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CustomerConfirmedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FinalApprovedAmount { get; set; }

        [StringLength(500)]
        public string? MechanicNotes { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public string? VerifiedById { get; set; }

        public DateTime? VerificationDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        [ForeignKey(nameof(VerifiedById))]
        public ApplicationUser? VerifiedBy { get; set; }
    }
}
