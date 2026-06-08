namespace JAA.Services
{
    public static class GeoService
    {
        private const double EarthRadiusKm = 6371.0;

        public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        // Estimated drive time assuming 40 km/h average city speed
        public static int EstimatedArrivalMin(double distanceKm) =>
            Math.Max(1, (int)Math.Ceiling(distanceKm / 40.0 * 60));

        public static string FormatDistance(double km) =>
            km < 1 ? $"{(int)(km * 1000)} m" : $"{km:F1} km";

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
