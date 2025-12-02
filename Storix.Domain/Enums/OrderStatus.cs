namespace Storix.Domain.Enums
{
    /// <summary>
    ///     Represents the status of an order through its lifecycle.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Order is being created/edited or confirmed but not yet in progress
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Order is approved and actively being processed
        /// FOR SO: Stock is RESERVED at this stage
        /// </summary>
        Active = 1,

        /// <summary>
        /// Order ready for inventory update
        /// FOR PO: Goods received - prompts inventory increase
        /// FOR SO: Goods shipped - prompts inventory decrease
        /// </summary>
        Fulfilled = 2,

        /// <summary>
        /// Order fully completed and closed
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Order cancelled
        /// If stock was reserved (Active SO), it gets released
        /// </summary>
        Cancelled = 4
    }
}
