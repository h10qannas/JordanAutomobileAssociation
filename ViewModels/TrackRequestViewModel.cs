using JAA.Models;

namespace JAA.ViewModels
{
    public class TrackRequestViewModel
    {
        public ServiceRequest Request { get; set; } = null!;
        public int CurrentStepIndex { get; set; }
        public List<(string Label, string Icon, string Description)> Steps { get; set; } = new();
    }
}
