using JAA.Models;

namespace JAA.ViewModels
{
    public class ShopDashboardViewModel
    {
        public RepairShop Shop { get; set; } = null!;
        public int PendingRequests { get; set; }
        public int ActiveJobs { get; set; }
        public int CompletedToday { get; set; }
        public decimal TotalEarnings { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int ApprovedMechanics { get; set; }
        public int AvailableMechanics { get; set; }
        public bool IsShopAvailable { get; set; }
        public List<ServiceRequest> IncomingRequests { get; set; } = new();
        public List<ServiceRequest> CurrentActiveJobs { get; set; } = new();
    }
}
