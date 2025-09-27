using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Categories
{
    public static class CategoryDtoMapper
    {
        public static CategoryDto ToDto( this Category category ) => new()
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsDeleted = category.IsDeleted,
            DeletedAt = category.DeletedAt
        };

        public static Category ToDomain( this CategoryDto dto ) => new(
            dto.CategoryId,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId,
            dto.ImageUrl,
            dto.IsDeleted,
            dto.DeletedAt
        );

        public static CreateCategoryDto ToCreateDto( this CategoryDto dto ) => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentCategoryId = dto.ParentCategoryId,
            ImageUrl = dto.ImageUrl,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt
        };

        public static UpdateCategoryDto ToUpdateDto( this CategoryDto dto ) => new()
        {
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            ParentCategoryId = dto.ParentCategoryId,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt
        };

        public static CreateCategoryDto ToCreateDto( this Category category ) => new()
        {
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsDeleted = category.IsDeleted,
            DeletedAt = category.DeletedAt
        };

        public static Category ToDomain( this CreateCategoryDto dto ) => new(
            0,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId,
            dto.ImageUrl,
            dto.IsDeleted,
            dto.DeletedAt
        );

        public static Category ToDomain( this UpdateCategoryDto dto ) => new(
            dto.CategoryId,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId,
            dto.ImageUrl,
            dto.IsDeleted,
            dto.DeletedAt
        );

        public static IEnumerable<CategoryDto> ToDto( this IEnumerable<Category> categories ) => categories.Select(c => c.ToDto());
    }
}
