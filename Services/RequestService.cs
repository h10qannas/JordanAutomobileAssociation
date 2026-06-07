using JAA.Data;
using JAA.Models;
using Microsoft.EntityFrameworkCore;

namespace JAA.Services
{
    public class RequestService
    {
        private readonly AppDbContext _db;

        public RequestService(AppDbContext db) => _db = db;

        public async Task<List<ServiceRequest>> GetCustomerRequestsAsync(string customerId) =>
            await _db.ServiceRequests
                .Where(r => r.CustomerId == customerId)
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .Include(r => r.Feedback)
                .Include(r => r.Payment)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairPayment)
                .Include(r => r.RepairQuotation)
                .Include(r => r.Invoice)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<ServiceRequest?> GetRequestByIdAsync(int id) =>
            await _db.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .Include(r => r.Feedback)
                .Include(r => r.Payment)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairQuotation)
                    .ThenInclude(q => q!.Mechanic)
                .Include(r => r.RepairPayment)
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<ServiceRequest?> GetActiveRequestAsync(string customerId) =>
            await _db.ServiceRequests
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .Include(r => r.InspectionPayment)
                .Where(r => r.CustomerId == customerId &&
                            r.Status != RequestStatus.Completed &&
                            r.Status != RequestStatus.Cancelled &&
                            r.Status != RequestStatus.Rejected &&
                            r.Status != RequestStatus.QuotationRejected)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

        public async Task<ServiceRequest> CreateRequestAsync(ServiceRequest request)
        {
            _db.ServiceRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<bool> CancelRequestAsync(int id, string customerId)
        {
            var request = await _db.ServiceRequests
                .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);

            if (request == null) return false;

            // Allow cancel only if still pending or inspection-paid (before shop accepts)
            if (request.Status != RequestStatus.Pending &&
                request.Status != RequestStatus.InspectionPaid)
                return false;

            request.Status    = RequestStatus.Cancelled;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
