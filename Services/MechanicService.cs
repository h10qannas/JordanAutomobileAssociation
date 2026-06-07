using JAA.Data;
using JAA.Models;
using Microsoft.EntityFrameworkCore;

namespace JAA.Services
{
    public class MechanicService
    {
        private readonly AppDbContext _db;

        public MechanicService(AppDbContext db) => _db = db;

        public async Task<List<Mechanic>> GetShopMechanicsAsync(int shopId) =>
            await _db.Mechanics
                .Where(m => m.ShopId == shopId)
                .OrderBy(m => m.FullName)
                .ToListAsync();

        public async Task<List<Mechanic>> GetApprovedAvailableMechanicsAsync(int shopId) =>
            await _db.Mechanics
                .Where(m => m.ShopId == shopId &&
                            m.Status == MechanicStatus.Approved &&
                            m.IsAvailable)
                .OrderBy(m => m.FullName)
                .ToListAsync();

        public async Task<Mechanic?> GetMechanicByIdAsync(int id) =>
            await _db.Mechanics
                .Include(m => m.Shop)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<bool> IsShopAvailableAsync(int shopId)
        {
            return await _db.Mechanics
                .AnyAsync(m => m.ShopId == shopId &&
                               m.Status == MechanicStatus.Approved &&
                               m.IsAvailable);
        }

        public async Task<int> GetAvailableMechanicCountAsync(int shopId) =>
            await _db.Mechanics
                .CountAsync(m => m.ShopId == shopId &&
                                 m.Status == MechanicStatus.Approved &&
                                 m.IsAvailable);

        public async Task<double> GetMechanicAverageRatingAsync(int mechanicId)
        {
            var ratings = await _db.Feedbacks
                .Where(f => f.MechanicId == mechanicId)
                .Select(f => f.Rating)
                .ToListAsync();
            return ratings.Count > 0 ? ratings.Average() : 0;
        }

        public async Task<bool> NationalIdExistsAsync(string nationalId, int? excludeId = null) =>
            await _db.Mechanics
                .AnyAsync(m => m.NationalId == nationalId && (excludeId == null || m.Id != excludeId));

        public async Task<Mechanic> AddMechanicAsync(Mechanic mechanic)
        {
            _db.Mechanics.Add(mechanic);
            await _db.SaveChangesAsync();
            return mechanic;
        }

        public async Task UpdateMechanicAsync(Mechanic mechanic)
        {
            _db.Mechanics.Update(mechanic);
            await _db.SaveChangesAsync();
        }

        public async Task SetMechanicAvailabilityAsync(int mechanicId, bool isAvailable)
        {
            var mechanic = await _db.Mechanics.FindAsync(mechanicId);
            if (mechanic != null)
            {
                mechanic.IsAvailable = isAvailable;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Mechanic>> GetPendingMechanicsAsync() =>
            await _db.Mechanics
                .Where(m => m.Status == MechanicStatus.Pending)
                .Include(m => m.Shop)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

        public async Task ApproveMechanicAsync(int mechanicId)
        {
            var mechanic = await _db.Mechanics.FindAsync(mechanicId);
            if (mechanic != null)
            {
                mechanic.Status = MechanicStatus.Approved;
                await _db.SaveChangesAsync();
            }
        }

        public async Task RejectMechanicAsync(int mechanicId)
        {
            var mechanic = await _db.Mechanics.FindAsync(mechanicId);
            if (mechanic != null)
            {
                mechanic.Status = MechanicStatus.Rejected;
                await _db.SaveChangesAsync();
            }
        }
    }
}
