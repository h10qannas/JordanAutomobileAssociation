using JAA.Models;

namespace JAA.ViewModels
{
    public class InspectionPaymentViewModel
    {
        public int ServiceRequestId { get; set; }
        public decimal Fee { get; set; }
        public decimal ShopShare { get; set; }
        public decimal PlatformShare { get; set; }
        public string SituationDescription { get; set; } = string.Empty;
        public PaymentMethod SelectedMethod { get; set; } = PaymentMethod.Cash;
    }

    public class SimulatedPaymentViewModel
    {
        public int ServiceRequestId { get; set; }
        public int? InspectionPaymentId { get; set; }
        public int? RepairPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = string.Empty; // "Inspection" or "Repair"
        public string Description { get; set; } = string.Empty;
    }

    public class RepairPaymentViewModel
    {
        public int ServiceRequestId { get; set; }
        public int QuotationId { get; set; }
        public decimal QuotedAmount { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal ShopAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string DiagnosisNotes { get; set; } = string.Empty;
        public string MechanicName { get; set; } = string.Empty;
        public PaymentMethod SelectedMethod { get; set; } = PaymentMethod.Cash;
    }

    public class RefundListViewModel
    {
        public List<Refund> PendingRefunds { get; set; } = new();
        public List<Refund> ProcessedRefunds { get; set; } = new();
    }
}
