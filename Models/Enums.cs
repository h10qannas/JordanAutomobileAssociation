namespace JAA.Models
{
    public enum RequestStatus
    {
        Pending                  = 0,
        Accepted                 = 1,
        MechanicArrived          = 2,
        InProgress               = 3,  // legacy — kept for backward compatibility
        Completed                = 4,
        Cancelled                = 5,
        Rejected                 = 6,
        InspectionPaid           = 7,  // inspection fee paid; awaiting shop acceptance
        Diagnosed                = 8,  // mechanic submitted repair quotation
        QuotationApproved        = 9,  // customer approved repair quotation + paid
        QuotationRejected        = 10, // customer rejected repair quotation
        AwaitingConfirmation     = 11, // mechanic reported final amount; customer must confirm
        UnderReview              = 12  // dispute flagged; admin reviewing
    }

    public enum VerificationStatus
    {
        Pending     = 0,
        Verified    = 1,
        Flagged     = 2,
        UnderReview = 3,
        Resolved    = 4
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

    public enum TestimonialStatus
    {
        Pending  = 0,
        Approved = 1,
        Rejected = 2,
        Hidden   = 3
    }

    public enum DeclineReason
    {
        TooExpensive      = 0,
        RepairElsewhere   = 1,
        NotUrgent         = 2,
        NeedSecondOpinion = 3,
        Other             = 4
    }
}
