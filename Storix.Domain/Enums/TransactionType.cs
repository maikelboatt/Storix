namespace Storix.Domain.Enums
{
    public enum TransactionType
    {
        /// <summary>Stock increase (purchases, returns to inventory, corrections)</summary>
        StockIn = 0,

        /// <summary>Stock decrease (sales, waste, corrections)</summary>
        StockOut = 1,

        /// <summary>Manual stock adjustment (positive or negative)</summary>
        Adjustment = 2,

        /// <summary>Transfer between locations</summary>
        Transfer = 3,

        /// <summary>Customer return (increases inventory)</summary>
        Return = 4,

        /// <summary>Damaged goods (decreases inventory)</summary>
        Damaged = 5,

        /// <summary>Lost/stolen goods (decreases inventory)</summary>
        Lost = 6
    }
}
