namespace Storix.Domain.Interfaces
{
    public interface ISoftDeletable
    {
        public bool IsDeleted { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}
