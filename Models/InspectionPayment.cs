using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class InspectionPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShopShare { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformShare { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(200)]
        public string? TransactionReference { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        public Refund? Refund { get; set; }
    }
}
