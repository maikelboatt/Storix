namespace Storix.Application.DTO.Categories
{
    public class CategoryListDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentCategory { get; set; }
        public string? ImageUrl { get; set; }
    }
}
