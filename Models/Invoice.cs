using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JAA.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int ServiceRequestId { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(ServiceRequestId))]
        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}
