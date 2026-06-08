using JAA.Models;

namespace JAA.ViewModels
{
    public class NearbyShop
    {
        public RepairShop Shop { get; set; } = null!;
        public double? DistanceKm { get; set; }
        public int? EstimatedArrivalMin { get; set; }
        public double AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public int AvailableMechanics { get; set; }
        public bool IsAvailable { get; set; }

        public string DistanceLabel => DistanceKm.HasValue
            ? Services.GeoService.FormatDistance(DistanceKm.Value) : "";
        public string EtaLabel => EstimatedArrivalMin.HasValue
            ? $"~{EstimatedArrivalMin} min" : "";
    }

    public class ShopsViewModel
    {
        public List<NearbyShop> Shops { get; set; } = new();
        public string? SelectedCity { get; set; }
        public List<string> Cities { get; set; } = new();
        public double? CustomerLat { get; set; }
        public double? CustomerLng { get; set; }
        public bool HasLocation => CustomerLat.HasValue && CustomerLng.HasValue;
    }
}
