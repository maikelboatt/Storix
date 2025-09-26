using System.Collections.Generic;
using Storix.Application.DTO.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Categories
{
    public interface ICategoryStore
    {
        // Store Initialization
        void Initialize( IEnumerable<Category> categories );

        void Clear();

        // Basic CRUD Operations
        CategoryDto? Create( int categoryId, CategoryDto categoryDto );

        CategoryDto? GetById( int categoryId );

        CategoryDto? Update( CategoryDto categoryDto ); // No need for categoryId parameter

        bool Delete( int categoryId );

        // Query Operations
        List<CategoryDto> GetAll( int? parentId = null, string? search = null, int skip = 0, int take = 100 );

        List<CategoryDto> GetChildren( int parentCategoryId );

        List<CategoryDto> GetRootCategories();

        // Utility Operations
        bool Exists( int categoryId );

        int GetCount( int? parentId = null );

        bool HasChildren( int categoryId );

        IEnumerable<Category> SearchCategories( string? searchTerm = null, bool? isActive = null );

        IEnumerable<Category> GetActiveCategories();
    }
}
