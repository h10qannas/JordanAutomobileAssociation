using JAA.Models;

namespace JAA.ViewModels
{
    public class AdminRequestsViewModel
    {
        public List<ServiceRequest> Requests { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }
    }
}
