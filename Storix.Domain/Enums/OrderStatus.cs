namespace Storix.Domain.Enums
{
    /// <summary>
    ///     Represents the status of an order through its lifecycle.
    /// </summary>
    public enum OrderStatus
    {
        Draft,
        Pending,
        Confirmed,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Returned
    }
}
