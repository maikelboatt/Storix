using System.Collections.Generic;
using Storix.Application.DTO.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Categories
{
    public interface ICategoryStore
    {
        void Initialize( IEnumerable<Category> categories );

        void Clear();

        CategoryDto? Create( int categoryId, CreateCategoryDto createDto );

        CategoryDto? Update( UpdateCategoryDto updateDto );

        bool Delete( int categoryId );

        CategoryDto? GetById( int categoryId );

        List<CategoryDto> GetAll(
            int? parentId = null,
            string? search = null,
            int skip = 0,
            int take = 100 );

        List<CategoryDto> GetChildren( int parentCategoryId );

        List<CategoryDto> GetRootCategories();

        IEnumerable<Category> GetActiveCategories();

        bool Exists( int categoryId );

        int GetCount( int? parentId = null );

        int GetActiveCount();

        bool HasChildren( int categoryId );

        IEnumerable<Category> SearchCategories( string? searchTerm = null );
    }
}
