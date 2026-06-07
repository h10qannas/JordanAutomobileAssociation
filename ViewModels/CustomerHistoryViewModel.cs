using JAA.Models;

namespace JAA.ViewModels
{
    public class CustomerHistoryViewModel
    {
        public List<ServiceRequest> Requests { get; set; } = new();
    }
}
