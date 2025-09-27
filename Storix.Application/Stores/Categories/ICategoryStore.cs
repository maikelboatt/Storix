using System.Collections.Generic;
using Storix.Application.DTO.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Categories
{
    public interface ICategoryStore
    {
        void Initialize( IEnumerable<Category> categories );

        void Clear();

        CategoryDto? Create( int categoryId, CategoryDto categoryDto );

        CategoryDto? Update( CategoryDto categoryDto );

        bool Delete( int categoryId );

        IEnumerable<Category> GetActiveCategories();

        CategoryDto? GetById( int categoryId, bool includeDeleted = false );

        bool SoftDelete( int categoryId );

        bool Restore( int categoryId );

        List<CategoryDto> GetAll( int? parentId = null, string? search = null, bool includeDeleted = false, int skip = 0, int take = 100 );

        List<CategoryDto> GetChildren( int parentCategoryId, bool includeDeleted = false );

        List<CategoryDto> GetRootCategories( bool includeDeleted = false );

        List<CategoryDto> GetDeletedCategories();

        bool Exists( int categoryId, bool includeDeleted = false );

        bool IsDeleted( int categoryId );

        int GetCount( int? parentId = null, bool includeDeleted = false );

        int GetActiveCount();

        int GetDeletedCount();

        int GetTotalCount();

        bool HasChildren( int categoryId, bool includeDeleted = false );

        IEnumerable<Category> SearchCategories( string? searchTerm = null, bool includeDeleted = false );
    }
}
