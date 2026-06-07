using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class RepairPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int QuotationId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedAmount { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal CommissionRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShopAmount { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(200)]
        public string? TransactionReference { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;

        [ForeignKey(nameof(QuotationId))]
        public RepairQuotation Quotation { get; set; } = null!;

        public Refund? Refund { get; set; }
    }
}
