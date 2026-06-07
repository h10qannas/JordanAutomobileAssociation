namespace JAA.ViewModels
{
    public class ShopMapMarker
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double AvgRating { get; set; }
        public int ReviewCount { get; set; }
        public int AvailableMechanics { get; set; }
        public bool IsAvailable { get; set; }
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }

    public class CustomerMapViewModel
    {
        public List<ShopMapMarker> Shops { get; set; } = new();
        public double? CustomerLat { get; set; }
        public double? CustomerLng { get; set; }
    }
}
