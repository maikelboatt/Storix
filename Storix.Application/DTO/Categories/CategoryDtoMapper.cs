using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Domain.Models;

namespace Storix.Application.DTO.Categories
{
    public static class CategoryDtoMapper
    {
        // Domain to DTO - excludes soft delete properties
        public static CategoryDto ToDto( this Category category ) => new()
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            Description = category.Description,
            ImageUrl = category.ImageUrl
        };

        // DTO to Domain - sets soft delete properties to defaults
        public static Category ToDomain( this CategoryDto dto ) => new(
            dto.CategoryId,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId,
            dto.ImageUrl,
            false,
            null
        );

        // CategoryDto to CreateCategoryDto
        public static CreateCategoryDto ToCreateDto( this CategoryDto dto ) => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentCategoryId = dto.ParentCategoryId,
            ImageUrl = dto.ImageUrl
        };

        // CategoryDto to UpdateCategoryDto
        public static UpdateCategoryDto ToUpdateDto( this CategoryDto dto ) => new()
        {
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            ParentCategoryId = dto.ParentCategoryId,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl
        };

        // Category to CreateCategoryDto
        public static CreateCategoryDto ToCreateDto( this Category category ) => new()
        {
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            Description = category.Description,
            ImageUrl = category.ImageUrl
        };

        // CreateCategoryDto to Domain - always creates non-deleted categories
        public static Category ToDomain( this CreateCategoryDto dto, int categoryId = 0 ) => new(
            categoryId,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId,
            dto.ImageUrl,
            false,
            null
        );

        // UpdateCategoryDto to Domain - preserves existing soft delete state
        public static Category ToDomain( this UpdateCategoryDto dto, bool isDeleted = false, DateTime? deletedAt = null ) => new(
            dto.CategoryId,
            dto.Name,
            dto.Description,
            dto.ParentCategoryId,
            dto.ImageUrl,
            isDeleted,
            deletedAt
        );

        // Collection mapping
        public static IEnumerable<CategoryDto> ToDto( this IEnumerable<Category> categories ) => categories.Select(c => c.ToDto());
    }
}
