using JAA.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JAA.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<RepairShop>        RepairShops        { get; set; }
        public DbSet<Mechanic>          Mechanics          { get; set; }
        public DbSet<ServiceRequest>    ServiceRequests    { get; set; }
        public DbSet<Payment>           Payments           { get; set; }
        public DbSet<InspectionPayment> InspectionPayments { get; set; }
        public DbSet<RepairQuotation>   RepairQuotations   { get; set; }
        public DbSet<RepairPayment>     RepairPayments     { get; set; }
        public DbSet<Refund>            Refunds            { get; set; }
        public DbSet<Invoice>           Invoices           { get; set; }
        public DbSet<Feedback>              Feedbacks              { get; set; }
        public DbSet<Testimonial>           Testimonials           { get; set; }
        public DbSet<PaymentVerification>   PaymentVerifications   { get; set; }
        public DbSet<SystemSetting>         SystemSettings         { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── ApplicationUser ────────────────────────────────────────────
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.OwnedShop)
                .WithOne(s => s.Owner)
                .HasForeignKey<RepairShop>(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.ServiceRequests)
                .WithOne(r => r.Customer)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Feedbacks)
                .WithOne(f => f.Customer)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── RepairShop ─────────────────────────────────────────────────
            builder.Entity<RepairShop>()
                .HasMany(s => s.Mechanics)
                .WithOne(m => m.Shop)
                .HasForeignKey(m => m.ShopId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RepairShop>()
                .HasMany(s => s.ServiceRequests)
                .WithOne(r => r.Shop)
                .HasForeignKey(r => r.ShopId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<RepairShop>()
                .HasMany(s => s.Feedbacks)
                .WithOne(f => f.Shop)
                .HasForeignKey(f => f.ShopId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Mechanic ───────────────────────────────────────────────────
            builder.Entity<Mechanic>()
                .HasIndex(m => m.NationalId)
                .IsUnique();

            builder.Entity<Mechanic>()
                .HasMany(m => m.ServiceRequests)
                .WithOne(r => r.Mechanic)
                .HasForeignKey(r => r.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Mechanic>()
                .HasMany(m => m.Feedbacks)
                .WithOne(f => f.Mechanic)
                .HasForeignKey(f => f.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Mechanic>()
                .HasMany(m => m.Quotations)
                .WithOne(q => q.Mechanic)
                .HasForeignKey(q => q.MechanicId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── ServiceRequest ─────────────────────────────────────────────
            builder.Entity<ServiceRequest>()
                .HasOne(r => r.Payment)
                .WithOne(p => p.ServiceRequest)
                .HasForeignKey<Payment>(p => p.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceRequest>()
                .HasOne(r => r.InspectionPayment)
                .WithOne(p => p.ServiceRequest)
                .HasForeignKey<InspectionPayment>(p => p.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceRequest>()
                .HasOne(r => r.RepairQuotation)
                .WithOne(q => q.ServiceRequest)
                .HasForeignKey<RepairQuotation>(q => q.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceRequest>()
                .HasOne(r => r.RepairPayment)
                .WithOne(p => p.ServiceRequest)
                .HasForeignKey<RepairPayment>(p => p.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceRequest>()
                .HasOne(r => r.Invoice)
                .WithOne(i => i.ServiceRequest)
                .HasForeignKey<Invoice>(i => i.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceRequest>()
                .HasOne(r => r.Feedback)
                .WithOne(f => f.ServiceRequest)
                .HasForeignKey<Feedback>(f => f.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Testimonial ────────────────────────────────────────────────
            builder.Entity<ServiceRequest>()
                .HasOne(r => r.Testimonial)
                .WithOne(t => t.ServiceRequest)
                .HasForeignKey<Testimonial>(t => t.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Testimonial>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Testimonial>()
                .HasOne(t => t.Shop)
                .WithMany()
                .HasForeignKey(t => t.ShopId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Testimonial>()
                .HasOne(t => t.Mechanic)
                .WithMany()
                .HasForeignKey(t => t.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Testimonial>()
                .HasOne(t => t.ApprovedBy)
                .WithMany()
                .HasForeignKey(t => t.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            // ── RepairQuotation → RepairPayment ────────────────────────────
            builder.Entity<RepairQuotation>()
                .HasOne(q => q.RepairPayment)
                .WithOne(p => p.Quotation)
                .HasForeignKey<RepairPayment>(p => p.QuotationId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Refund ─────────────────────────────────────────────────────
            builder.Entity<Refund>()
                .HasOne(r => r.InspectionPayment)
                .WithOne(p => p.Refund)
                .HasForeignKey<Refund>(r => r.InspectionPaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Refund>()
                .HasOne(r => r.RepairPayment)
                .WithOne(p => p.Refund)
                .HasForeignKey<Refund>(r => r.RepairPaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Refund>()
                .HasOne(r => r.ServiceRequest)
                .WithMany()
                .HasForeignKey(r => r.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Refund>()
                .HasOne(r => r.ProcessedByAdmin)
                .WithMany()
                .HasForeignKey(r => r.ProcessedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── PaymentVerification ────────────────────────────────────────
            builder.Entity<ServiceRequest>()
                .HasOne(r => r.PaymentVerification)
                .WithOne(v => v.ServiceRequest)
                .HasForeignKey<PaymentVerification>(v => v.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PaymentVerification>()
                .HasOne(v => v.VerifiedBy)
                .WithMany()
                .HasForeignKey(v => v.VerifiedById)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Invoice unique index ───────────────────────────────────────
            builder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // ── Seed SystemSettings ────────────────────────────────────────
            builder.Entity<SystemSetting>().HasData(
                new SystemSetting { Id = 1, Key = "PlatformName",         Value = "JAA — Jordan Auto Assistance" },
                new SystemSetting { Id = 2, Key = "SupportEmail",         Value = "help@jaa.jo" },
                new SystemSetting { Id = 3, Key = "SupportPhone",         Value = "+962 6 000 0000" },
                new SystemSetting { Id = 4, Key = "InspectionFee",        Value = "5.00" },
                new SystemSetting { Id = 5, Key = "ShopInspectionShare",   Value = "4.00" },
                new SystemSetting { Id = 6, Key = "PlatformInspectionShare", Value = "1.00" },
                new SystemSetting { Id = 7, Key = "RepairCommissionRate",  Value = "0.15" },
                new SystemSetting { Id = 8, Key = "VatRate",              Value = "0.16" }
            );
        }
    }
}
