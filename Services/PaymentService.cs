using JAA.Data;
using JAA.Models;
using Microsoft.EntityFrameworkCore;

namespace JAA.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _db;

        public PaymentService(AppDbContext db) => _db = db;

        public async Task<(decimal Fee, decimal ShopShare, decimal PlatformShare)> GetInspectionFeeSettingsAsync()
        {
            var settings = await _db.SystemSettings
                .Where(s => s.Key == "InspectionFee" ||
                            s.Key == "ShopInspectionShare" ||
                            s.Key == "PlatformInspectionShare")
                .ToListAsync();

            var fee      = decimal.Parse(settings.FirstOrDefault(s => s.Key == "InspectionFee")?.Value ?? "5.00");
            var shopShare = decimal.Parse(settings.FirstOrDefault(s => s.Key == "ShopInspectionShare")?.Value ?? "4.00");
            var platShare = decimal.Parse(settings.FirstOrDefault(s => s.Key == "PlatformInspectionShare")?.Value ?? "1.00");
            return (fee, shopShare, platShare);
        }

        public async Task<(decimal CommissionRate, decimal VatRate)> GetRepairRatesAsync()
        {
            var settings = await _db.SystemSettings
                .Where(s => s.Key == "RepairCommissionRate" || s.Key == "VatRate")
                .ToListAsync();

            var commission = decimal.Parse(settings.FirstOrDefault(s => s.Key == "RepairCommissionRate")?.Value ?? "0.15");
            var vat        = decimal.Parse(settings.FirstOrDefault(s => s.Key == "VatRate")?.Value ?? "0.16");
            return (commission, vat);
        }

        public async Task<InspectionPayment> CreateInspectionPaymentAsync(
            int requestId, PaymentMethod method, string? transactionRef = null)
        {
            var (fee, shopShare, platShare) = await GetInspectionFeeSettingsAsync();

            var payment = new InspectionPayment
            {
                ServiceRequestId   = requestId,
                Amount             = fee,
                ShopShare          = shopShare,
                PlatformShare      = platShare,
                Method             = method,
                Status             = method == PaymentMethod.Cash ? PaymentStatus.Paid : PaymentStatus.Pending,
                TransactionReference = transactionRef,
                PaidAt             = method == PaymentMethod.Cash ? DateTime.UtcNow : null,
                CreatedAt          = DateTime.UtcNow
            };

            _db.InspectionPayments.Add(payment);
            await _db.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> ConfirmOnlineInspectionPaymentAsync(int paymentId, string transactionRef)
        {
            var payment = await _db.InspectionPayments.FindAsync(paymentId);
            if (payment == null || payment.Status == PaymentStatus.Paid) return false;

            payment.Status               = PaymentStatus.Paid;
            payment.TransactionReference = transactionRef;
            payment.PaidAt               = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<RepairPayment> CreateRepairPaymentAsync(
            int requestId, int quotationId, decimal quotedAmount,
            PaymentMethod method, string? transactionRef = null)
        {
            var (commissionRate, vatRate) = await GetRepairRatesAsync();

            var commissionAmount = Math.Round(quotedAmount * commissionRate, 2);
            var shopAmount       = Math.Round(quotedAmount - commissionAmount, 2);
            var vatAmount        = Math.Round(quotedAmount * vatRate, 2);
            var totalAmount      = Math.Round(quotedAmount + vatAmount, 2);

            var payment = new RepairPayment
            {
                ServiceRequestId     = requestId,
                QuotationId          = quotationId,
                QuotedAmount         = quotedAmount,
                CommissionRate       = commissionRate,
                CommissionAmount     = commissionAmount,
                ShopAmount           = shopAmount,
                VatRate              = vatRate,
                VatAmount            = vatAmount,
                TotalAmount          = totalAmount,
                Method               = method,
                Status               = method == PaymentMethod.Cash ? PaymentStatus.Paid : PaymentStatus.Pending,
                TransactionReference = transactionRef,
                PaidAt               = method == PaymentMethod.Cash ? DateTime.UtcNow : null,
                CreatedAt            = DateTime.UtcNow
            };

            _db.RepairPayments.Add(payment);
            await _db.SaveChangesAsync();
            return payment;
        }

        public async Task<bool> ConfirmOnlineRepairPaymentAsync(int paymentId, string transactionRef)
        {
            var payment = await _db.RepairPayments.FindAsync(paymentId);
            if (payment == null || payment.Status == PaymentStatus.Paid) return false;

            payment.Status               = PaymentStatus.Paid;
            payment.TransactionReference = transactionRef;
            payment.PaidAt               = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<InspectionPayment?> GetInspectionPaymentByRequestAsync(int requestId) =>
            await _db.InspectionPayments
                .Include(p => p.ServiceRequest)
                .FirstOrDefaultAsync(p => p.ServiceRequestId == requestId);

        public async Task<RepairPayment?> GetRepairPaymentByRequestAsync(int requestId) =>
            await _db.RepairPayments
                .Include(p => p.ServiceRequest)
                .Include(p => p.Quotation)
                .FirstOrDefaultAsync(p => p.ServiceRequestId == requestId);

        public async Task<Refund> CreateRefundRequestAsync(
            int requestId, RefundType type,
            int? inspectionPaymentId, int? repairPaymentId,
            decimal amount, string reason)
        {
            var refund = new Refund
            {
                ServiceRequestId    = requestId,
                Type                = type,
                InspectionPaymentId = inspectionPaymentId,
                RepairPaymentId     = repairPaymentId,
                Amount              = amount,
                Reason              = reason,
                Status              = RefundStatus.Pending,
                RequestedAt         = DateTime.UtcNow
            };

            _db.Refunds.Add(refund);
            await _db.SaveChangesAsync();
            return refund;
        }

        public async Task<List<Refund>> GetPendingRefundsAsync() =>
            await _db.Refunds
                .Where(r => r.Status == RefundStatus.Pending)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.Customer)
                .Include(r => r.InspectionPayment)
                .Include(r => r.RepairPayment)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

        public async Task<bool> ProcessRefundAsync(int refundId, string adminId, bool approve, string? notes)
        {
            var refund = await _db.Refunds.FindAsync(refundId);
            if (refund == null || refund.Status != RefundStatus.Pending) return false;

            refund.Status             = approve ? RefundStatus.Approved : RefundStatus.Rejected;
            refund.ProcessedByAdminId = adminId;
            refund.AdminNotes         = notes;
            refund.ProcessedAt        = DateTime.UtcNow;

            if (approve)
            {
                if (refund.InspectionPaymentId.HasValue)
                {
                    var ip = await _db.InspectionPayments.FindAsync(refund.InspectionPaymentId);
                    if (ip != null) ip.Status = PaymentStatus.Refunded;
                }
                if (refund.RepairPaymentId.HasValue)
                {
                    var rp = await _db.RepairPayments.FindAsync(refund.RepairPaymentId);
                    if (rp != null) rp.Status = PaymentStatus.Refunded;
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetShopTotalInspectionEarningsAsync(int shopId)
        {
            var result = await _db.InspectionPayments
                .Where(p => p.ServiceRequest.ShopId == shopId && p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.ShopShare) ?? 0;
            return result;
        }

        public async Task<decimal> GetShopTotalRepairEarningsAsync(int shopId)
        {
            var result = await _db.RepairPayments
                .Where(p => p.ServiceRequest.ShopId == shopId && p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.ShopAmount) ?? 0;
            return result;
        }
    }
}
