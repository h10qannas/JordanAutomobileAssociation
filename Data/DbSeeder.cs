using JAA.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JAA.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db          = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await db.Database.MigrateAsync();

            // ── Roles ──────────────────────────────────────────────────────
            foreach (var role in new[] { "Admin", "Customer", "ShopOwner" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Admin ───────────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("admin@jaa.jo") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@jaa.jo", Email = "admin@jaa.jo",
                    FullName = "JAA Administrator", City = "Amman",
                    Role = UserRole.Admin, EmailConfirmed = true, IsActive = true
                };
                var r = await userManager.CreateAsync(admin, "Admin@123");
                if (r.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Customer ────────────────────────────────────────────────────
            if (await userManager.FindByEmailAsync("customer@jaa.jo") == null)
            {
                var cust = new ApplicationUser
                {
                    UserName = "customer@jaa.jo", Email = "customer@jaa.jo",
                    FullName = "Sara Ahmad", City = "Amman",
                    PhoneNumber = "+962791001001",
                    Role = UserRole.Customer, EmailConfirmed = true, IsActive = true
                };
                var r = await userManager.CreateAsync(cust, "Customer@123");
                if (r.Succeeded) await userManager.AddToRoleAsync(cust, "Customer");
            }

            // ── ShopOwner 1 ─────────────────────────────────────────────────
            ApplicationUser? owner1 = await userManager.FindByEmailAsync("owner1@jaa.jo");
            if (owner1 == null)
            {
                owner1 = new ApplicationUser
                {
                    UserName = "owner1@jaa.jo", Email = "owner1@jaa.jo",
                    FullName = "Khaled Al-Rashid", City = "Amman",
                    PhoneNumber = "+962772002001",
                    Role = UserRole.ShopOwner, EmailConfirmed = true, IsActive = true
                };
                var r = await userManager.CreateAsync(owner1, "Owner@123");
                if (r.Succeeded) await userManager.AddToRoleAsync(owner1, "ShopOwner");
            }

            // ── ShopOwner 2 ─────────────────────────────────────────────────
            ApplicationUser? owner2 = await userManager.FindByEmailAsync("owner2@jaa.jo");
            if (owner2 == null)
            {
                owner2 = new ApplicationUser
                {
                    UserName = "owner2@jaa.jo", Email = "owner2@jaa.jo",
                    FullName = "Lina Mansour", City = "Amman",
                    PhoneNumber = "+962783003001",
                    Role = UserRole.ShopOwner, EmailConfirmed = true, IsActive = true
                };
                var r = await userManager.CreateAsync(owner2, "Owner@123");
                if (r.Succeeded) await userManager.AddToRoleAsync(owner2, "ShopOwner");
            }

            // ── Repair Shops ────────────────────────────────────────────────
            if (!await db.RepairShops.AnyAsync())
            {
                var shop1 = new RepairShop
                {
                    OwnerId     = owner1!.Id,
                    ShopName    = "Al-Ameen Auto Services",
                    OwnerName   = "Khaled Al-Rashid",
                    Description = "Full-service auto repair center with 15 years of expertise. Certified mechanics, modern diagnostic equipment, and transparent pricing. We come to you or you come to us.",
                    Address     = "King Abdullah II St., Shmeisani",
                    City        = "Amman",
                    PhoneNumber = "+96265661234",
                    Latitude    = 31.9804,
                    Longitude   = 35.8784,
                    LogoUrl     = "/img/service-1.jpg",
                    IsVerified  = true,
                    ShopStatus  = ShopStatus.Approved
                };

                var shop2 = new RepairShop
                {
                    OwnerId     = owner2!.Id,
                    ShopName    = "Golden Wrench Garage",
                    OwnerName   = "Lina Mansour",
                    Description = "Your trusted neighborhood garage in Sweifieh. We handle everything from roadside emergencies to full engine overhauls. Fast response, fair prices.",
                    Address     = "Mecca St., Sweifieh",
                    City        = "Amman",
                    PhoneNumber = "+96265819876",
                    Latitude    = 31.9655,
                    Longitude   = 35.8690,
                    LogoUrl     = "/img/service-2.jpg",
                    IsVerified  = true,
                    ShopStatus  = ShopStatus.Approved
                };

                db.RepairShops.AddRange(shop1, shop2);
                await db.SaveChangesAsync();

                // ── Mechanics ────────────────────────────────────────────────
                var mech1 = new Mechanic
                {
                    ShopId            = shop1.Id,
                    FullName          = "Ahmad Khalil",
                    NationalId        = "9876543210",
                    PhoneNumber       = "+962791234567",
                    YearsOfExperience = 8,
                    Status            = MechanicStatus.Approved,
                    IsAvailable       = true
                };

                var mech2 = new Mechanic
                {
                    ShopId            = shop1.Id,
                    FullName          = "Omar Nasser",
                    NationalId        = "8765432109",
                    PhoneNumber       = "+962792345678",
                    YearsOfExperience = 5,
                    Status            = MechanicStatus.Approved,
                    IsAvailable       = true
                };

                var mech3 = new Mechanic
                {
                    ShopId            = shop2.Id,
                    FullName          = "Hassan Ibrahim",
                    NationalId        = "7654321098",
                    PhoneNumber       = "+962793456789",
                    YearsOfExperience = 10,
                    Status            = MechanicStatus.Approved,
                    IsAvailable       = true
                };

                db.Mechanics.AddRange(mech1, mech2, mech3);
                await db.SaveChangesAsync();

                // ── Service Requests (2 completed) ─────────────────────────
                var customer = await userManager.FindByEmailAsync("customer@jaa.jo");

                var req1 = new ServiceRequest
                {
                    CustomerId           = customer!.Id,
                    ShopId               = shop1.Id,
                    MechanicId           = mech1.Id,
                    SituationDescription = "Car won't start, engine makes a clicking noise",
                    DiagnosisNotes       = "Dead battery with corroded terminals. No charge remaining.",
                    Resolution           = "Replaced battery on-site. Car started immediately.",
                    Status               = RequestStatus.Completed,
                    CustomerLatitude     = 31.9804,
                    CustomerLongitude    = 35.8784,
                    CreatedAt            = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt            = DateTime.UtcNow.AddDays(-10)
                };

                var req2 = new ServiceRequest
                {
                    CustomerId           = customer!.Id,
                    ShopId               = shop2.Id,
                    MechanicId           = mech3.Id,
                    SituationDescription = "Overheating warning light came on while driving on the highway",
                    DiagnosisNotes       = "Coolant level critically low, small leak in upper radiator hose.",
                    Resolution           = "Patched hose, refilled coolant. Advised full hose replacement within 2 weeks.",
                    Status               = RequestStatus.Completed,
                    CustomerLatitude     = 31.9655,
                    CustomerLongitude    = 35.8690,
                    CreatedAt            = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt            = DateTime.UtcNow.AddDays(-5)
                };

                db.ServiceRequests.AddRange(req1, req2);
                await db.SaveChangesAsync();

                // ── Inspection Payments ─────────────────────────────────────
                db.InspectionPayments.AddRange(
                    new InspectionPayment
                    {
                        ServiceRequestId = req1.Id,
                        Amount           = 5.00m,
                        ShopShare        = 4.00m,
                        PlatformShare    = 1.00m,
                        Method           = PaymentMethod.Cash,
                        Status           = PaymentStatus.Paid,
                        PaidAt           = DateTime.UtcNow.AddDays(-10)
                    },
                    new InspectionPayment
                    {
                        ServiceRequestId = req2.Id,
                        Amount           = 5.00m,
                        ShopShare        = 4.00m,
                        PlatformShare    = 1.00m,
                        Method           = PaymentMethod.Cash,
                        Status           = PaymentStatus.Paid,
                        PaidAt           = DateTime.UtcNow.AddDays(-5)
                    }
                );

                // ── Repair Quotations ────────────────────────────────────────
                var quotation1 = new RepairQuotation
                {
                    ServiceRequestId = req1.Id,
                    MechanicId       = mech1.Id,
                    DiagnosisNotes   = "Dead battery with corroded terminals. No charge remaining.",
                    QuotedAmount     = 35.00m,
                    Status           = QuotationStatus.Approved,
                    CustomerResponseAt = DateTime.UtcNow.AddDays(-10)
                };

                var quotation2 = new RepairQuotation
                {
                    ServiceRequestId = req2.Id,
                    MechanicId       = mech3.Id,
                    DiagnosisNotes   = "Coolant level critically low, small leak in upper radiator hose.",
                    QuotedAmount     = 20.00m,
                    Status           = QuotationStatus.Approved,
                    CustomerResponseAt = DateTime.UtcNow.AddDays(-5)
                };

                db.RepairQuotations.AddRange(quotation1, quotation2);
                await db.SaveChangesAsync();

                // ── Repair Payments ─────────────────────────────────────────
                db.RepairPayments.AddRange(
                    new RepairPayment
                    {
                        ServiceRequestId = req1.Id,
                        QuotationId      = quotation1.Id,
                        QuotedAmount     = 35.00m,
                        CommissionRate   = 0.15m,
                        CommissionAmount = 5.25m,
                        ShopAmount       = 29.75m,
                        VatRate          = 0.16m,
                        VatAmount        = 5.60m,
                        TotalAmount      = 40.60m,
                        Method           = PaymentMethod.Cash,
                        Status           = PaymentStatus.Paid,
                        PaidAt           = DateTime.UtcNow.AddDays(-10)
                    },
                    new RepairPayment
                    {
                        ServiceRequestId = req2.Id,
                        QuotationId      = quotation2.Id,
                        QuotedAmount     = 20.00m,
                        CommissionRate   = 0.15m,
                        CommissionAmount = 3.00m,
                        ShopAmount       = 17.00m,
                        VatRate          = 0.16m,
                        VatAmount        = 3.20m,
                        TotalAmount      = 23.20m,
                        Method           = PaymentMethod.Cash,
                        Status           = PaymentStatus.Paid,
                        PaidAt           = DateTime.UtcNow.AddDays(-5)
                    }
                );

                // ── Legacy Payments (backward compat) ───────────────────────
                db.Payments.AddRange(
                    new Payment
                    {
                        ServiceRequestId = req1.Id,
                        Amount  = 35m,
                        Notes   = "Battery replacement (Varta 60Ah) + labor",
                        PaidAt  = DateTime.UtcNow.AddDays(-10)
                    },
                    new Payment
                    {
                        ServiceRequestId = req2.Id,
                        Amount  = 20m,
                        Notes   = "Hose patch + coolant refill",
                        PaidAt  = DateTime.UtcNow.AddDays(-5)
                    }
                );

                // ── Invoices ────────────────────────────────────────────────
                db.Invoices.AddRange(
                    new Invoice
                    {
                        InvoiceNumber    = $"INV-{DateTime.UtcNow.Year}-00001",
                        ServiceRequestId = req1.Id,
                        IssuedAt         = DateTime.UtcNow.AddDays(-10)
                    },
                    new Invoice
                    {
                        InvoiceNumber    = $"INV-{DateTime.UtcNow.Year}-00002",
                        ServiceRequestId = req2.Id,
                        IssuedAt         = DateTime.UtcNow.AddDays(-5)
                    }
                );

                // ── Feedbacks ──────────────────────────────────────────────
                db.Feedbacks.AddRange(
                    new Feedback
                    {
                        ServiceRequestId = req1.Id,
                        CustomerId       = customer.Id,
                        ShopId           = shop1.Id,
                        MechanicId       = mech1.Id,
                        Rating           = 5,
                        Comment          = "Excellent! Ahmad arrived in 20 minutes and fixed my car on the spot. Very professional.",
                        CreatedAt        = DateTime.UtcNow.AddDays(-9)
                    },
                    new Feedback
                    {
                        ServiceRequestId = req2.Id,
                        CustomerId       = customer.Id,
                        ShopId           = shop2.Id,
                        MechanicId       = mech3.Id,
                        Rating           = 4,
                        Comment          = "Good and honest mechanic. Hassan diagnosed the problem quickly. Price was fair.",
                        CreatedAt        = DateTime.UtcNow.AddDays(-4)
                    }
                );

                await db.SaveChangesAsync();
            }
        }
    }
}
