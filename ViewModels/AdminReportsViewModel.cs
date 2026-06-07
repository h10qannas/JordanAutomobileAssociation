using JAA.Models;

namespace JAA.ViewModels
{
    public class AdminReportsViewModel
    {
        public List<ServiceRequest> CompletedRequests { get; set; } = new();
        public List<Feedback> RecentFeedbacks { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public int TotalCompletedRequests { get; set; }
        public double AverageRating { get; set; }
    }
}
