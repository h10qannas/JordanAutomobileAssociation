using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class RepairQuotation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int MechanicId { get; set; }

        [Required]
        [StringLength(1000)]
        public string DiagnosisNotes { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedAmount { get; set; }

        public QuotationStatus Status { get; set; } = QuotationStatus.AwaitingApproval;

        public DateTime? CustomerResponseAt { get; set; }

        [StringLength(500)]
        public string? CustomerRejectionReason { get; set; }

        public DeclineReason? DeclineReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        [ForeignKey(nameof(MechanicId))]
        public Mechanic Mechanic { get; set; } = null!;

        public RepairPayment? RepairPayment { get; set; }
    }
}
