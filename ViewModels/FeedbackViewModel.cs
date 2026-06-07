using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class FeedbackViewModel
    {
        [Required]
        public int ServiceRequestId { get; set; }

        [Required][Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public string ShopName { get; set; } = string.Empty;
        public string? MechanicName { get; set; }
        public string SituationDescription { get; set; } = string.Empty;
    }
}
