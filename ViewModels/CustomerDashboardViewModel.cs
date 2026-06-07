using JAA.Models;

namespace JAA.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public string UserFullName { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int PendingRequests { get; set; }
        public ServiceRequest? ActiveRequest { get; set; }
        public List<ServiceRequest> RecentRequests { get; set; } = new();
    }
}
