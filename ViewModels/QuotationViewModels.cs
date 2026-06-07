using System.ComponentModel.DataAnnotations;
using JAA.Models;

namespace JAA.ViewModels
{
    public class SubmitQuotationViewModel
    {
        public int ServiceRequestId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string SituationDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Diagnosis notes are required")]
        [StringLength(1000)]
        [Display(Name = "Diagnosis Notes")]
        public string DiagnosisNotes { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quoted amount is required")]
        [Range(0.01, 100000)]
        [Display(Name = "Repair Cost (JOD)")]
        public decimal QuotedAmount { get; set; }
    }

    public class QuotationApprovalViewModel
    {
        public int ServiceRequestId { get; set; }
        public int QuotationId { get; set; }
        public string DiagnosisNotes { get; set; } = string.Empty;
        public decimal QuotedAmount { get; set; }
        public decimal InspectionAlreadyPaid { get; set; }
        public string MechanicName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string SituationDescription { get; set; } = string.Empty;
        public PaymentMethod SelectedRepairMethod { get; set; } = PaymentMethod.Cash;

        // VAT preview
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalWithVat { get; set; }
    }

    public class AssignMechanicViewModel
    {
        public int ServiceRequestId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string SituationDescription { get; set; } = string.Empty;
        public double CustomerLatitude { get; set; }
        public double CustomerLongitude { get; set; }
        public List<Mechanic> AvailableMechanics { get; set; } = new();
        public int SelectedMechanicId { get; set; }
    }
}
