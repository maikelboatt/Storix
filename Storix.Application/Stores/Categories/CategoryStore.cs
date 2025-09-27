using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Categories
{
    public class CategoryStore:ICategoryStore
    {
        private readonly Dictionary<int, Category> _categories;
        private readonly Dictionary<int, Category> _deletedCategories;

        public CategoryStore( List<Category>? initialCategories = null )
        {
            _categories = new Dictionary<int, Category>();
            _deletedCategories = new Dictionary<int, Category>();

            if (initialCategories == null) return;

            foreach (Category category in initialCategories)
            {
                if (category.IsDeleted)
                {
                    _deletedCategories[category.CategoryId] = category;
                }
                else
                {
                    _categories[category.CategoryId] = category;
                }
            }
        }

        public void Initialize( IEnumerable<Category> categories )
        {
            // Clear existing data first
            _categories.Clear();
            _deletedCategories.Clear();

            // Add categories to appropriate dictionaries based on deletion status
            foreach (Category category in categories)
            {
                if (category.IsDeleted)
                {
                    _deletedCategories[category.CategoryId] = category;
                }
                else
                {
                    _categories[category.CategoryId] = category;
                }
            }
        }

        public void Clear()
        {
            _categories.Clear();
            _deletedCategories.Clear();
        }

        public CategoryDto? Create( int categoryId, CategoryDto categoryDto )
        {
            // Simple validation - return null if invalid
            if (string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return null;
            }

            // Check if parent exists and is active (not deleted)
            if (categoryDto.ParentCategoryId.HasValue && !Exists(categoryDto.ParentCategoryId.Value, false))
            {
                return null;
            }

            Category category = new(
                categoryId,
                categoryDto.Name.Trim(),
                categoryDto.Description?.Trim(),
                categoryDto.ParentCategoryId,
                categoryDto.ImageUrl?.Trim()
                // IsDeleted defaults to false
                // DeletedAt defaults to null
            );

            _categories[categoryId] = category;
            return category.ToDto();
        }

        public CategoryDto? Update( CategoryDto categoryDto )
        {
            // Try to find the category in either collection
            Category? existingCategory = null;
            bool isCurrentlyDeleted = false;

            if (_categories.TryGetValue(categoryDto.CategoryId, out existingCategory))
            {
                isCurrentlyDeleted = false;
            }
            else if (_deletedCategories.TryGetValue(categoryDto.CategoryId, out existingCategory))
            {
                isCurrentlyDeleted = true;
            }
            else
            {
                return null; // Category not found
            }

            if (string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return null; // Invalid name
            }

            // Check parent exists and is active (only check active categories for parent validation)
            if (categoryDto.ParentCategoryId.HasValue)
            {
                if (!Exists(categoryDto.ParentCategoryId.Value, false))
                {
                    return null; // Parent not found or deleted
                }

                if (WouldCreateCircularReference(categoryDto.CategoryId, categoryDto.ParentCategoryId.Value))
                {
                    return null; // Would create circular reference
                }
            }

            Category updatedCategory = existingCategory with
            {
                Name = categoryDto.Name.Trim(),
                Description = categoryDto.Description?.Trim(),
                ParentCategoryId = categoryDto.ParentCategoryId,
                ImageUrl = categoryDto.ImageUrl?.Trim(),
                // Preserve ISoftDeletable properties from DTO
                IsDeleted = categoryDto.IsDeleted,
                DeletedAt = categoryDto.DeletedAt
            };

            // Move category between collections if deletion status changed
            if (isCurrentlyDeleted && !updatedCategory.IsDeleted)
            {
                // Moving from deleted to active
                _deletedCategories.Remove(categoryDto.CategoryId);
                _categories[categoryDto.CategoryId] = updatedCategory;
            }
            else if (!isCurrentlyDeleted && updatedCategory.IsDeleted)
            {
                // Moving from active to deleted
                _categories.Remove(categoryDto.CategoryId);
                _deletedCategories[categoryDto.CategoryId] = updatedCategory;
            }
            else
            {
                // Status hasn't changed, update in current collection
                if (isCurrentlyDeleted)
                {
                    _deletedCategories[categoryDto.CategoryId] = updatedCategory;
                }
                else
                {
                    _categories[categoryDto.CategoryId] = updatedCategory;
                }
            }

            return updatedCategory.ToDto();
        }

        public bool Delete( int categoryId )
        {
            // Remove from both collections to handle any case
            bool removedFromActive = _categories.Remove(categoryId);
            bool removedFromDeleted = _deletedCategories.Remove(categoryId);

            return removedFromActive || removedFromDeleted;
        }

        public IEnumerable<Category> GetActiveCategories()
        {
            return _categories.Values
                              .OrderBy(c => c.Name)
                              .ToList();
        }

        public CategoryDto? GetById( int categoryId, bool includeDeleted = false )
        {
            if (_categories.TryGetValue(categoryId, out Category? category))
            {
                return category.ToDto();
            }

            if (includeDeleted && _deletedCategories.TryGetValue(categoryId, out Category? deletedCategory))
            {
                return deletedCategory.ToDto();
            }

            return null;
        }

        public bool SoftDelete( int categoryId )
        {
            if (!_categories.TryGetValue(categoryId, out Category? category))
            {
                return false; // Category not found or already deleted
            }

            // Check if category has active children
            if (HasChildren(categoryId, false))
            {
                return false; // Has active children, cannot delete
            }

            // Move category to deleted collection
            Category softDeletedCategory = category with
            {
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            };

            _categories.Remove(categoryId);
            _deletedCategories[categoryId] = softDeletedCategory;
            return true;
        }

        public bool Restore( int categoryId )
        {
            if (!_deletedCategories.TryGetValue(categoryId, out Category? category))
            {
                return false; // Category not found or not deleted
            }

            // Check if parent still exists and is active
            if (category.ParentCategoryId.HasValue && !Exists(category.ParentCategoryId.Value, false))
            {
                return false; // Cannot restore because parent is deleted or doesn't exist
            }

            // Move category back to active collection
            Category restoredCategory = category with
            {
                IsDeleted = false,
                DeletedAt = null
            };

            _deletedCategories.Remove(categoryId);
            _categories[categoryId] = restoredCategory;
            return true;
        }

        public List<CategoryDto> GetAll( int? parentId = null, string? search = null, bool includeDeleted = false, int skip = 0, int take = 100 )
        {
            IEnumerable<Category> categories = includeDeleted
                ? _categories.Values.Concat(_deletedCategories.Values)
                : _categories.Values;

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

        public List<CategoryDto> GetChildren( int parentCategoryId, bool includeDeleted = false ) => GetAll(parentCategoryId, includeDeleted: includeDeleted);

        public List<CategoryDto> GetRootCategories( bool includeDeleted = false )
        {
            IEnumerable<Category> categories = includeDeleted
                ? _categories.Values.Concat(_deletedCategories.Values)
                : _categories.Values;

            return categories
                   .Where(c => c.ParentCategoryId == null)
                   .OrderBy(c => c.Name)
                   .Select(c => c.ToDto())
                   .ToList();
        }

        public List<CategoryDto> GetDeletedCategories()
        {
            return _deletedCategories.Values
                                     .OrderBy(c => c.Name)
                                     .Select(c => c.ToDto())
                                     .ToList();
        }

        public bool Exists( int categoryId, bool includeDeleted = false )
        {
            bool existsInActive = _categories.ContainsKey(categoryId);
            return existsInActive || includeDeleted && _deletedCategories.ContainsKey(categoryId);
        }

        public bool IsDeleted( int categoryId ) => _deletedCategories.ContainsKey(categoryId);

        public int GetCount( int? parentId = null, bool includeDeleted = false )
        {
            IEnumerable<Category> categories = includeDeleted
                ? _categories.Values.Concat(_deletedCategories.Values)
                : _categories.Values;

            if (parentId.HasValue)
            {
                return categories.Count(c => c.ParentCategoryId == parentId.Value);
            }
            return categories.Count();
        }

        public int GetActiveCount() => _categories.Count;

        public int GetDeletedCount() => _deletedCategories.Count;

        public int GetTotalCount() => _categories.Count + _deletedCategories.Count;

        public bool HasChildren( int categoryId, bool includeDeleted = false )
        {
            IEnumerable<Category> categories = includeDeleted
                ? _categories.Values.Concat(_deletedCategories.Values)
                : _categories.Values;

            return categories.Any(c => c.ParentCategoryId == categoryId);
        }

        public IEnumerable<Category> SearchCategories( string? searchTerm = null, bool includeDeleted = false )
        {
            IEnumerable<Category> query = includeDeleted
                ? _categories.Values.Concat(_deletedCategories.Values)
                : _categories.Values;

            // Apply search term filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                        (c.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return query.OrderBy(c => c.Name).ToList();
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

                CategoryDto? category = GetById(currentId, false); // Only check active categories for hierarchy validation
                if (category?.ParentCategoryId == null) break;
                currentId = category.ParentCategoryId.Value;
            }

            return false;
        }
    }
}
