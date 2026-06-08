using JAA.Models;

namespace JAA.ViewModels
{
    public class AdminReportsViewModel
    {
        public List<ServiceRequest> CompletedRequests { get; set; } = new();
        public List<Feedback> RecentFeedbacks { get; set; } = new();

        // Platform revenue (JAA's cut only)
        public decimal TotalRevenue { get; set; }

        // Repair breakdown
        public decimal TotalRepairRevenue { get; set; }     // sum of QuotedAmounts (what customers paid)
        public decimal TotalJAACommission { get; set; }     // sum of CommissionAmounts (JAA's portion)
        public decimal TotalShopRevenue { get; set; }       // sum of ShopAmounts (shops' net)

        // Inspection platform share
        public decimal TotalInspectionPlatformRevenue { get; set; }

        public int TotalCompletedRequests { get; set; }
        public double AverageRating { get; set; }
    }
}
