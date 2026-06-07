using JAA.Models;

namespace JAA.ViewModels
{
    public class ShopDetailViewModel
    {
        public RepairShop Shop { get; set; } = null!;
        public List<Feedback> Feedbacks { get; set; } = new();
        public double AverageRating { get; set; }
        public int CompletedJobs { get; set; }
    }
}
