using JAA.Data;
using JAA.Models;
using JAA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace JAA.Services
{
    public class TestimonialService
    {
        private readonly AppDbContext _db;

        public TestimonialService(AppDbContext db) => _db = db;

        // ── Helpers ────────────────────────────────────────────────────────

        private static TestimonialListItemViewModel ToViewModel(Testimonial t) => new()
        {
            Id              = t.Id,
            CustomerName    = t.Customer?.FullName ?? "Customer",
            Title           = t.Title,
            Message         = t.Message,
            Rating          = t.Rating,
            ShopName        = t.Shop?.ShopName ?? "Shop",
            MechanicName    = t.Mechanic?.FullName,
            Status          = t.Status,
            IsFeatured      = t.IsFeatured,
            CreatedAt       = t.CreatedAt,
            ApprovedAt      = t.ApprovedAt,
            ServiceDate     = t.ServiceRequest?.CreatedAt ?? t.CreatedAt,
            RejectionReason = t.RejectionReason
        };

        private IQueryable<Testimonial> BaseQuery() =>
            _db.Testimonials
               .Include(t => t.Customer)
               .Include(t => t.Shop)
               .Include(t => t.Mechanic)
               .Include(t => t.ServiceRequest);

        // ── Public / homepage ──────────────────────────────────────────────

        public async Task<List<TestimonialListItemViewModel>> GetHomepageTestimonialsAsync(int count = 9)
        {
            var list = await BaseQuery()
                .Where(t => t.Status == TestimonialStatus.Approved)
                .OrderByDescending(t => t.IsFeatured)
                .ThenByDescending(t => t.ApprovedAt)
                .Take(count)
                .ToListAsync();

            return list.Select(ToViewModel).ToList();
        }

        public async Task<(double AvgRating, int TotalApproved, int TotalCompleted)> GetPlatformStatsAsync()
        {
            var approved = await _db.Testimonials
                .Where(t => t.Status == TestimonialStatus.Approved)
                .ToListAsync();

            var avgRating     = approved.Any() ? approved.Average(t => (double)t.Rating) : 0;
            var totalApproved = approved.Count;
            var totalCompleted = await _db.ServiceRequests
                .CountAsync(r => r.Status == RequestStatus.Completed);

            return (Math.Round(avgRating, 1), totalApproved, totalCompleted);
        }

        // ── Shop profile ───────────────────────────────────────────────────

        public async Task<List<TestimonialListItemViewModel>> GetShopTestimonialsAsync(int shopId)
        {
            var list = await BaseQuery()
                .Where(t => t.ShopId == shopId && t.Status == TestimonialStatus.Approved)
                .OrderByDescending(t => t.IsFeatured)
                .ThenByDescending(t => t.ApprovedAt)
                .ToListAsync();

            return list.Select(ToViewModel).ToList();
        }

        public async Task<(double AvgRating, int Count)> GetShopTestimonialStatsAsync(int shopId)
        {
            var list = await _db.Testimonials
                .Where(t => t.ShopId == shopId && t.Status == TestimonialStatus.Approved)
                .ToListAsync();

            var avg = list.Any() ? list.Average(t => (double)t.Rating) : 0;
            return (Math.Round(avg, 1), list.Count);
        }

        // ── Customer ───────────────────────────────────────────────────────

        public async Task<MyTestimonialsViewModel> GetMyTestimonialsAsync(string customerId)
        {
            var testimonials = await BaseQuery()
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Completed requests that belong to this customer and have no testimonial yet
            var requestIdsWithTestimonials = testimonials.Select(t => t.ServiceRequestId).ToHashSet();

            var eligible = await _db.ServiceRequests
                .Where(r => r.CustomerId == customerId &&
                            r.Status == RequestStatus.Completed &&
                            r.ShopId != null &&
                            !requestIdsWithTestimonials.Contains(r.Id))
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            return new MyTestimonialsViewModel
            {
                Testimonials = testimonials.Select(ToViewModel).ToList(),
                EligibleRequests = eligible.Select(r => new EligibleRequestViewModel
                {
                    RequestId            = r.Id,
                    ShopName             = r.Shop?.ShopName ?? "Shop",
                    MechanicName         = r.Mechanic?.FullName,
                    MechanicId           = r.MechanicId,
                    ShopId               = r.ShopId!.Value,
                    CompletedAt          = r.UpdatedAt,
                    SituationDescription = r.SituationDescription
                }).ToList()
            };
        }

        public async Task<bool> CanSubmitAsync(string customerId, int requestId)
        {
            var request = await _db.ServiceRequests
                .FirstOrDefaultAsync(r => r.Id == requestId &&
                                          r.CustomerId == customerId &&
                                          r.Status == RequestStatus.Completed &&
                                          r.ShopId != null);
            if (request == null) return false;

            var exists = await _db.Testimonials
                .AnyAsync(t => t.ServiceRequestId == requestId);
            return !exists;
        }

        public async Task<(bool Success, string? Error)> SubmitAsync(SubmitTestimonialViewModel vm, string customerId)
        {
            if (!await CanSubmitAsync(customerId, vm.ServiceRequestId))
                return (false, "You cannot submit a testimonial for this request.");

            _db.Testimonials.Add(new Testimonial
            {
                CustomerId       = customerId,
                ServiceRequestId = vm.ServiceRequestId,
                ShopId           = vm.ShopId,
                MechanicId       = vm.MechanicId,
                Title            = vm.Title?.Trim(),
                Message          = vm.Message.Trim(),
                Rating           = vm.Rating,
                Status           = TestimonialStatus.Pending,
                IsFeatured       = false,
                CreatedAt        = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<EditTestimonialViewModel?> GetForEditAsync(int id, string customerId)
        {
            var t = await BaseQuery()
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (t == null || t.Status != TestimonialStatus.Pending) return null;

            return new EditTestimonialViewModel
            {
                Id        = t.Id,
                Title     = t.Title,
                Message   = t.Message,
                Rating    = t.Rating,
                ShopName  = t.Shop?.ShopName ?? "Shop",
                CreatedAt = t.CreatedAt
            };
        }

        public async Task<(bool Success, string? Error)> EditAsync(EditTestimonialViewModel vm, string customerId)
        {
            var t = await _db.Testimonials
                .FirstOrDefaultAsync(t => t.Id == vm.Id && t.CustomerId == customerId);

            if (t == null) return (false, "Testimonial not found.");
            if (t.Status != TestimonialStatus.Pending)
                return (false, "Approved testimonials cannot be edited.");

            t.Title   = vm.Title?.Trim();
            t.Message = vm.Message.Trim();
            t.Rating  = vm.Rating;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // ── Admin ──────────────────────────────────────────────────────────

        public async Task<AdminTestimonialsViewModel> GetAdminViewAsync(TestimonialStatus? filter)
        {
            var allQuery = BaseQuery().Include(t => t.ApprovedBy);

            var all = await allQuery.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var filtered = filter.HasValue
                ? all.Where(t => t.Status == filter.Value).ToList()
                : all;

            return new AdminTestimonialsViewModel
            {
                Testimonials  = filtered.Select(ToViewModel).ToList(),
                FilterStatus  = filter,
                PendingCount  = all.Count(t => t.Status == TestimonialStatus.Pending),
                ApprovedCount = all.Count(t => t.Status == TestimonialStatus.Approved),
                RejectedCount = all.Count(t => t.Status == TestimonialStatus.Rejected),
                FeaturedCount = all.Count(t => t.IsFeatured && t.Status == TestimonialStatus.Approved)
            };
        }

        public async Task<bool> ApproveAsync(int id, string adminId)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return false;

            t.Status          = TestimonialStatus.Approved;
            t.ApprovedAt      = DateTime.UtcNow;
            t.ApprovedById    = adminId;
            t.RejectionReason = null;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int id, string adminId, string? reason)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return false;

            t.Status          = TestimonialStatus.Rejected;
            t.ApprovedById    = adminId;
            t.RejectionReason = reason;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HideAsync(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return false;

            t.Status     = TestimonialStatus.Hidden;
            t.IsFeatured = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFeaturedAsync(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null || t.Status != TestimonialStatus.Approved) return false;

            t.IsFeatured = !t.IsFeatured;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var t = await _db.Testimonials.FindAsync(id);
            if (t == null) return false;

            _db.Testimonials.Remove(t);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
