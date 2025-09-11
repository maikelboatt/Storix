using Storix.Domain.Enums;

namespace Storix.Application.DTO.InventoryTransaction
{
    public class CreateInventoryTransactionDto
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
