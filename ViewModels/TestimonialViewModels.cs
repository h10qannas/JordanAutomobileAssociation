using System.ComponentModel.DataAnnotations;
using JAA.Models;

namespace JAA.ViewModels
{
    public class SubmitTestimonialViewModel
    {
        public int ServiceRequestId { get; set; }
        public int ShopId           { get; set; }
        public int? MechanicId      { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Please share your experience.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Message must be between 20 and 2000 characters.")]
        public string Message { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select a rating.")]
        public int Rating { get; set; } = 5;

        // Display info (not posted back)
        public string ShopName             { get; set; } = string.Empty;
        public string? MechanicName        { get; set; }
        public string SituationDescription { get; set; } = string.Empty;
        public DateTime ServiceDate        { get; set; }
    }

    public class EditTestimonialViewModel
    {
        public int Id { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Please share your experience.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Message must be between 20 and 2000 characters.")]
        public string Message { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select a rating.")]
        public int Rating { get; set; }

        // Display only
        public string ShopName  { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TestimonialListItemViewModel
    {
        public int    Id           { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Title       { get; set; }
        public string Message      { get; set; } = string.Empty;
        public int    Rating       { get; set; }
        public string ShopName     { get; set; } = string.Empty;
        public string? MechanicName { get; set; }
        public TestimonialStatus Status     { get; set; }
        public bool   IsFeatured   { get; set; }
        public DateTime CreatedAt  { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime ServiceDate { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class EligibleRequestViewModel
    {
        public int      RequestId            { get; set; }
        public string   ShopName             { get; set; } = string.Empty;
        public string?  MechanicName         { get; set; }
        public int?     MechanicId           { get; set; }
        public int      ShopId               { get; set; }
        public DateTime CompletedAt          { get; set; }
        public string   SituationDescription { get; set; } = string.Empty;
    }

    public class MyTestimonialsViewModel
    {
        public List<TestimonialListItemViewModel> Testimonials      { get; set; } = new();
        public List<EligibleRequestViewModel>     EligibleRequests  { get; set; } = new();
    }

    public class AdminTestimonialsViewModel
    {
        public List<TestimonialListItemViewModel> Testimonials  { get; set; } = new();
        public TestimonialStatus? FilterStatus { get; set; }
        public int PendingCount  { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int FeaturedCount { get; set; }
    }

    public class HomeIndexViewModel
    {
        public List<TestimonialListItemViewModel> Testimonials        { get; set; } = new();
        public double AvgRating                   { get; set; }
        public int    TotalTestimonials            { get; set; }
        public int    TotalCompletedServices       { get; set; }
    }

    public class ShopTestimonialsViewModel
    {
        public List<TestimonialListItemViewModel> Testimonials { get; set; } = new();
        public double AvgRating  { get; set; }
        public int    TotalCount { get; set; }
    }
}
