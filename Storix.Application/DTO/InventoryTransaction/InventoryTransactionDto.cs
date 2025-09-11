using Storix.Domain.Enums;
using System;

namespace Storix.Application.DTO.InventoryTransaction
{
    public class InventoryTransactionDto
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
