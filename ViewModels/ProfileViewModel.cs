using JAA.Models;
using System.ComponentModel.DataAnnotations;

namespace JAA.ViewModels
{
    public class ProfileViewModel
    {
        // ── Form fields ────────────────────────────────────────────────
        [Required][StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required][EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        // ── Display-only fields (not submitted by form) ────────────────
        public string City { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; }
        public bool IsActive { get; set; } = true;
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public List<ServiceRequest> RecentRequests { get; set; } = new();
    }
}
