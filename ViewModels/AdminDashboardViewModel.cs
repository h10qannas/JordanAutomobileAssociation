using JAA.Models;

namespace JAA.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalShops { get; set; }
        public int VerifiedShops { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalRequests { get; set; }
        public int ActiveRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int ActiveRequestsToday { get; set; }
        public int CompletedToday { get; set; }
        public double AverageRating { get; set; }
        public int PendingMechanicsCount { get; set; }
        public int PendingRefundsCount { get; set; }
        public int DeclinedRequests { get; set; }
        public double DeclinedRepairRate { get; set; }
        public List<ServiceRequest> LiveRequests { get; set; } = new();
        public List<RepairShop> UnverifiedShops { get; set; } = new();
    }
}
