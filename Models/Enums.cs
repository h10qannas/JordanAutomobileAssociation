namespace JAA.Models
{
    public enum RequestStatus
    {
        Pending           = 0,
        Accepted          = 1,
        MechanicArrived   = 2,
        InProgress        = 3,  // legacy — kept for backward compatibility
        Completed         = 4,
        Cancelled         = 5,
        Rejected          = 6,
        InspectionPaid    = 7,  // inspection fee paid; awaiting shop acceptance
        Diagnosed         = 8,  // mechanic submitted repair quotation
        QuotationApproved = 9,  // customer approved repair quotation + paid
        QuotationRejected = 10  // customer rejected repair quotation
    }

    public enum UserRole
    {
        Customer  = 0,
        ShopOwner = 1,
        Admin     = 2
    }

    public enum ShopStatus
    {
        Pending  = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum MechanicStatus
    {
        Pending  = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum PaymentMethod
    {
        Cash   = 0,
        Online = 1
    }

    public enum PaymentStatus
    {
        Pending  = 0,
        Paid     = 1,
        Refunded = 2,
        Failed   = 3
    }

    public enum QuotationStatus
    {
        AwaitingApproval = 0,
        Approved         = 1,
        Rejected         = 2
    }

    public enum RefundStatus
    {
        Pending  = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum RefundType
    {
        InspectionFee = 0,
        RepairPayment = 1
    }
}
