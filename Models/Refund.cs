using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class Refund
    {
        [Key]
        public int Id { get; set; }

        public RefundType Type { get; set; }

        public int? InspectionPaymentId { get; set; }

        public int? RepairPaymentId { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] 
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        public string? ProcessedByAdminId { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(InspectionPaymentId))]
        public InspectionPayment? InspectionPayment { get; set; }

        [ForeignKey(nameof(RepairPaymentId))]
        public RepairPayment? RepairPayment { get; set; }

        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        [ForeignKey(nameof(ProcessedByAdminId))]
        public ApplicationUser? ProcessedByAdmin { get; set; }
    }
}
