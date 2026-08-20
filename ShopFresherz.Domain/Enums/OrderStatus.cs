namespace ShopFresherz.Domain.Enums;

/// <summary>Represents the lifecycle states of an order in the state machine.</summary>
public enum OrderStatus
{
    /// <summary>Order created; stock reserved; awaiting payment initiation.</summary>
    Pending = 0,

    /// <summary>Payment flow started; 30-minute timeout active.</summary>
    AwaitingPayment = 1,

    /// <summary>Payment confirmed via gateway webhook; stock deducted.</summary>
    Paid = 2,

    /// <summary>Admin confirmed; fulfilment triggered.</summary>
    Processing = 3,

    /// <summary>Tracking number added; in transit.</summary>
    Shipped = 4,

    /// <summary>Delivery confirmed; loyalty points awarded.</summary>
    Delivered = 5,

    /// <summary>Order cancelled; refund initiated if paid; stock released.</summary>
    Cancelled = 6,

    /// <summary>Customer initiated return within 7 days of delivery.</summary>
    RefundRequested = 7,

    /// <summary>Refund approved; stock restocked; gateway refund processed.</summary>
    Refunded = 8,

    /// <summary>
    /// Order created in the two-step checkout flow; stock reserved; awaiting the
    /// frontend to complete the Flutterwave inline popup and call confirm-order.
    /// </summary>
    Draft = 9
}
