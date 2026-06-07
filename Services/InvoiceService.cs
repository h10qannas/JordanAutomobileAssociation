using JAA.Data;
using JAA.Models;
using Microsoft.EntityFrameworkCore;

namespace JAA.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _db;

        public InvoiceService(AppDbContext db) => _db = db;

        public async Task<Invoice> GetOrCreateInvoiceAsync(int requestId)
        {
            var existing = await _db.Invoices
                .FirstOrDefaultAsync(i => i.ServiceRequestId == requestId);

            if (existing != null) return existing;

            var year   = DateTime.UtcNow.Year;
            var count  = await _db.Invoices.CountAsync(i => i.IssuedAt.Year == year) + 1;
            var number = $"INV-{year}-{count:D5}";

            var invoice = new Invoice
            {
                InvoiceNumber   = number,
                ServiceRequestId = requestId,
                IssuedAt        = DateTime.UtcNow
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice;
        }

        public async Task<InvoiceData?> GetInvoiceDataAsync(int requestId)
        {
            var request = await _db.ServiceRequests
                .Include(r => r.Customer)
                .Include(r => r.Shop)
                .Include(r => r.Mechanic)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairQuotation)
                .Include(r => r.RepairPayment)
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return null;

            return new InvoiceData
            {
                Request           = request,
                InvoiceNumber     = request.Invoice?.InvoiceNumber ?? "DRAFT",
                IssuedAt          = request.Invoice?.IssuedAt ?? DateTime.UtcNow,
                CustomerName      = request.Customer.FullName,
                CustomerPhone     = request.Customer.PhoneNumber ?? "",
                ShopName          = request.Shop?.ShopName ?? "",
                ShopPhone         = request.Shop?.PhoneNumber ?? "",
                ShopCity          = request.Shop?.City ?? "",
                MechanicName      = request.Mechanic?.FullName ?? "",
                MechanicPhone     = request.Mechanic?.PhoneNumber ?? "",
                InspectionAmount  = request.InspectionPayment?.Amount ?? 0,
                InspectionMethod  = request.InspectionPayment?.Method,
                InspectionStatus  = request.InspectionPayment?.Status,
                RepairQuotedAmount = request.RepairPayment?.QuotedAmount ?? 0,
                CommissionRate    = request.RepairPayment?.CommissionRate ?? 0,
                CommissionAmount  = request.RepairPayment?.CommissionAmount ?? 0,
                ShopRepairAmount  = request.RepairPayment?.ShopAmount ?? 0,
                VatRate           = request.RepairPayment?.VatRate ?? 0,
                VatAmount         = request.RepairPayment?.VatAmount ?? 0,
                RepairTotalAmount = request.RepairPayment?.TotalAmount ?? 0,
                RepairMethod      = request.RepairPayment?.Method,
                RepairStatus      = request.RepairPayment?.Status,
                HasRepairPayment  = request.RepairPayment != null
            };
        }
    }

    public class InvoiceData
    {
        public ServiceRequest Request       { get; set; } = null!;
        public string InvoiceNumber         { get; set; } = string.Empty;
        public DateTime IssuedAt            { get; set; }
        public string CustomerName          { get; set; } = string.Empty;
        public string CustomerPhone         { get; set; } = string.Empty;
        public string ShopName              { get; set; } = string.Empty;
        public string ShopPhone             { get; set; } = string.Empty;
        public string ShopCity              { get; set; } = string.Empty;
        public string MechanicName          { get; set; } = string.Empty;
        public string MechanicPhone         { get; set; } = string.Empty;
        public decimal InspectionAmount     { get; set; }
        public PaymentMethod? InspectionMethod { get; set; }
        public PaymentStatus? InspectionStatus { get; set; }
        public decimal RepairQuotedAmount   { get; set; }
        public decimal CommissionRate       { get; set; }
        public decimal CommissionAmount     { get; set; }
        public decimal ShopRepairAmount     { get; set; }
        public decimal VatRate              { get; set; }
        public decimal VatAmount            { get; set; }
        public decimal RepairTotalAmount    { get; set; }
        public PaymentMethod? RepairMethod  { get; set; }
        public PaymentStatus? RepairStatus  { get; set; }
        public bool HasRepairPayment        { get; set; }
        public decimal GrandTotal           => InspectionAmount + RepairTotalAmount;
    }
}
