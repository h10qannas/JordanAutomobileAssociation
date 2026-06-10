using JAA.Data;
using JAA.Models;
using JAA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JAA.Services
{
    public class ShopService
    {
        private readonly AppDbContext _db;

        public ShopService(AppDbContext db) => _db = db;

        public async Task<List<RepairShop>> GetVerifiedShopsAsync(string? city = null) =>
            await _db.RepairShops
                .Where(s => s.ShopStatus == ShopStatus.Approved &&
                            (city == null || s.City == city))
                .Include(s => s.Feedbacks)
                .Include(s => s.Mechanics)
                .OrderByDescending(s => s.Feedbacks.Any()
                    ? s.Feedbacks.Average(f => (double)f.Rating) : 0)
                .ToListAsync();

        public async Task<List<NearbyShop>> GetNearbyShopsAsync(
            double? customerLat, double? customerLng,
            string? city = null, double radiusKm = 150)
        {
            var shops = await _db.RepairShops
                .Where(s => s.ShopStatus == ShopStatus.Approved &&
                            (city == null || s.City == city))
                .Include(s => s.Feedbacks)
                .Include(s => s.Mechanics)
                .ToListAsync();

            return shops
                .Select(s =>
                {
                    double? dist = null;
                    if (customerLat.HasValue && customerLng.HasValue
                        && s.Latitude.HasValue && s.Longitude.HasValue)
                        dist = GeoService.HaversineKm(
                            customerLat.Value, customerLng.Value,
                            s.Latitude.Value, s.Longitude.Value);

                    var availCount = s.Mechanics.Count(
                        m => m.Status == MechanicStatus.Approved && m.IsAvailable);
                    var avgRating = s.Feedbacks.Any()
                        ? s.Feedbacks.Average(f => (double)f.Rating) : 0.0;

                    return new NearbyShop
                    {
                        Shop                = s,
                        DistanceKm          = dist,
                        EstimatedArrivalMin = dist.HasValue
                            ? GeoService.EstimatedArrivalMin(dist.Value) : null,
                        AvgRating           = avgRating,
                        ReviewCount         = s.Feedbacks.Count,
                        AvailableMechanics  = availCount,
                        IsAvailable         = availCount > 0
                    };
                })
                .Where(ns => !customerLat.HasValue
                          || !ns.DistanceKm.HasValue
                          || ns.DistanceKm <= radiusKm)
                .OrderBy(ns => ns.DistanceKm ?? double.MaxValue)
                .ThenByDescending(ns => ns.IsAvailable)
                .ThenByDescending(ns => ns.AvgRating)
                .ToList();
        }

        public async Task<List<RepairShop>> GetAllShopsForMapAsync() =>
            await _db.RepairShops
                .Where(s => s.ShopStatus == ShopStatus.Approved &&
                            s.Latitude.HasValue && s.Longitude.HasValue)
                .Include(s => s.Feedbacks)
                .Include(s => s.Mechanics)
                .ToListAsync();

        public async Task<RepairShop?> GetShopByIdAsync(int id) =>
            await _db.RepairShops
                .Include(s => s.Owner)
                .Include(s => s.Feedbacks)
                    .ThenInclude(f => f.Customer)
                .Include(s => s.Feedbacks)
                    .ThenInclude(f => f.Mechanic)
                .Include(s => s.Mechanics)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<RepairShop?> GetShopByOwnerAsync(string ownerId) =>
            await _db.RepairShops
                .Include(s => s.Mechanics)
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId);

        public async Task<List<ServiceRequest>> GetIncomingRequestsAsync() =>
            await _db.ServiceRequests
                .Where(r => r.Status == RequestStatus.InspectionPaid && r.ShopId == null)
                .Include(r => r.Customer)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<ServiceRequest>> GetActiveJobsAsync(int shopId) =>
            await _db.ServiceRequests
                .Where(r => r.ShopId == shopId &&
                    (r.Status == RequestStatus.Accepted ||
                     r.Status == RequestStatus.MechanicArrived ||
                     r.Status == RequestStatus.InProgress ||
                     r.Status == RequestStatus.Diagnosed ||
                     r.Status == RequestStatus.QuotationApproved))
                .Include(r => r.Customer)
                .Include(r => r.Mechanic)
                .Include(r => r.RepairQuotation)
                .Include(r => r.InspectionPayment)
                .OrderBy(r => r.UpdatedAt)
                .ToListAsync();

        public async Task<List<ServiceRequest>> GetRejectedQuotationsAsync(int shopId) =>
            await _db.ServiceRequests
                .Where(r => r.ShopId == shopId && r.Status == RequestStatus.QuotationRejected)
                .Include(r => r.Customer)
                .Include(r => r.Mechanic)
                .Include(r => r.RepairQuotation)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

        public async Task<List<ServiceRequest>> GetCompletedJobsAsync(int shopId) =>
            await _db.ServiceRequests
                .Where(r => r.ShopId == shopId && r.Status == RequestStatus.Completed)
                .Include(r => r.Customer)
                .Include(r => r.Mechanic)
                .Include(r => r.Payment)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairPayment)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

        public async Task<double> GetAverageRatingAsync(int shopId)
        {
            var feedbacks = await _db.Feedbacks.Where(f => f.ShopId == shopId).ToListAsync();
            return feedbacks.Count > 0 ? feedbacks.Average(f => f.Rating) : 0;
        }

        public bool IsShopAvailable(RepairShop shop) =>
            shop.Mechanics.Any(m => m.Status == MechanicStatus.Approved && m.IsAvailable);

        public async Task<bool> IsShopAvailableByIdAsync(int shopId) =>
            await _db.Mechanics.AnyAsync(m =>
                m.ShopId == shopId &&
                m.Status == MechanicStatus.Approved &&
                m.IsAvailable);

        public async Task<List<RepairShop>> GetPendingShopsAsync() =>
            await _db.RepairShops
                .Where(s => s.ShopStatus == ShopStatus.Pending)
                .Include(s => s.Owner)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

        public async Task ApproveShopAsync(int shopId)
        {
            var shop = await _db.RepairShops.FindAsync(shopId);
            if (shop != null)
            {
                shop.ShopStatus = ShopStatus.Approved;
                shop.IsVerified = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task RejectShopAsync(int shopId, string? reason)
        {
            var shop = await _db.RepairShops.FindAsync(shopId);
            if (shop != null)
            {
                shop.ShopStatus     = ShopStatus.Rejected;
                shop.IsVerified     = false;
                shop.RejectionReason = reason;
                await _db.SaveChangesAsync();
            }
        }
    }
}
