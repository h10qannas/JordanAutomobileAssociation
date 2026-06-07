using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class RequestHelpViewModel
    {
        [Required]
        [Display(Name = "Describe your situation")]
        [StringLength(1000, MinimumLength = 5)]
        public string SituationDescription { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
}
