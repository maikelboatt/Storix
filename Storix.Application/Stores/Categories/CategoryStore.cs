using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Categories
{
    public class CategoryStore:ICategoryStore
    {
        private readonly Dictionary<int, Category> _categories;

        public CategoryStore( List<Category>? initialCategories = null )
        {
            _categories = new Dictionary<int, Category>();
            if (initialCategories == null) return;

            foreach (Category category in initialCategories)
            {
                _categories[category.CategoryId] = category;
            }
        }

        public void Initialize( IEnumerable<Category> categories )
        {
            // Clear existing data first
            _categories.Clear();

            // Add all categories to the store
            foreach (Category category in categories)
            {
                _categories[category.CategoryId] = category;
            }
        }

        public void Clear()
        {
            _categories.Clear();
        }

        public CategoryDto? Create( int categoryId, CategoryDto categoryDto )
        {
            // Simple validation - return null if invalid
            if (string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return null;
            }

            if (categoryDto.ParentCategoryId.HasValue && !Exists(categoryDto.ParentCategoryId.Value))
            {
                return null;
            }

            Category category = new(
                categoryId,
                categoryDto.Name.Trim(),
                categoryDto.Description?.Trim(),
                categoryDto.ParentCategoryId,
                categoryDto.ImageUrl?.Trim()
            );

            _categories[categoryId] = category;
            // return MapToDto(category);
            return category.ToDto();
        }

        public CategoryDto? GetById( int categoryId )
        {
            _categories.TryGetValue(categoryId, out Category? category);
            return category?.ToDto();
        }

        public CategoryDto? Update( CategoryDto categoryDto )
        {
            // Use the CategoryId from the DTO itself
            if (!_categories.TryGetValue(categoryDto.CategoryId, out Category? existingCategory))
            {
                return null; // Category not found
            }

            if (string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return null; // Invalid name
            }

            if (categoryDto.ParentCategoryId.HasValue)
            {
                if (!Exists(categoryDto.ParentCategoryId.Value))
                {
                    return null; // Parent not found
                }

                if (WouldCreateCircularReference(categoryDto.CategoryId, categoryDto.ParentCategoryId.Value))
                {
                    return null; // Would create circular reference
                }
            }

            Category updatedCategory = new(
                categoryDto.CategoryId,
                categoryDto.Name.Trim(),
                categoryDto.Description?.Trim(),
                categoryDto.ParentCategoryId,
                categoryDto.ImageUrl?.Trim()
            );

            _categories[categoryDto.CategoryId] = updatedCategory;
            return updatedCategory.ToDto();
        }

        public bool Delete( int categoryId )
        {
            if (!_categories.ContainsKey(categoryId))
            {
                return false; // Category not found
            }

            if (HasChildren(categoryId))
            {
                return false; // Has children, cannot delete
            }

            _categories.Remove(categoryId);
            return true; // Successfully deleted
        }

        public List<CategoryDto> GetAll( int? parentId = null, string? search = null, int skip = 0, int take = 100 )
        {
            IEnumerable<Category> categories = _categories.Values.AsEnumerable();

            // Filter by parent
            if (parentId.HasValue)
            {
                categories = categories.Where(cat => cat.ParentCategoryId == parentId.Value);
            }

            // Filter by search
            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLowerInvariant();
                categories = categories.Where(cat =>
                                                  cat.Name.ToLowerInvariant().Contains(searchLower) ||
                                                  cat.Description != null && cat.Description.ToLowerInvariant().Contains(searchLower));
            }

            return categories
                   .OrderBy(c => c.Name)
                   .Skip(skip)
                   .Take(take)
                   .Select(c => c.ToDto())
                   .ToList();
        }

        public List<CategoryDto> GetChildren( int parentCategoryId ) => GetAll(parentCategoryId);

        public List<CategoryDto> GetRootCategories()
        {
            return _categories.Values
                              .Where(c => c.ParentCategoryId == null)
                              .OrderBy(c => c.Name)
                              .Select(c => c.ToDto())
                              .ToList();
        }

        public bool Exists( int categoryId ) => _categories.ContainsKey(categoryId);

        public int GetCount( int? parentId = null )
        {
            if (parentId.HasValue)
            {
                return _categories.Values.Count(c => c.ParentCategoryId == parentId.Value);
            }
            return _categories.Count;
        }

        public bool HasChildren( int categoryId )
        {
            return _categories.Values.Any(c => c.ParentCategoryId == categoryId);
        }

        public IEnumerable<Category> SearchCategories( string? searchTerm = null, bool? isActive = null )
        {
            IEnumerable<Category> query = _categories.Values.AsEnumerable();

            // Apply search term filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                                        c.Description != null && (c.Name.Contains(searchTerm) ||
                                                                  c.Description.Contains(searchTerm)));
            }


            return query.OrderBy(c => c.Name).ToList();
        }

        public IEnumerable<Category> GetActiveCategories()
        {
            return _categories.Values
                              .OrderBy(c => c.Name)
                              .ToList();
        }


        private bool WouldCreateCircularReference( int categoryId, int parentId )
        {
            int currentId = parentId;

            while (currentId != 0)
            {
                if (currentId == categoryId)
                {
                    return true;
                }

                CategoryDto? category = GetById(currentId);
                if (category?.ParentCategoryId == null) break;
                currentId = category.ParentCategoryId.Value;
            }

            return false;
        }
    }
}
