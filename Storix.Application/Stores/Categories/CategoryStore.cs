using System;
using System.Collections.Generic;
using System.Linq;
using Storix.Application.DTO.Categories;
using Storix.Domain.Models;

namespace Storix.Application.Stores.Categories
{
    /// <summary>
    ///     In-memory cache for active (non-deleted) categories.
    ///     Provides fast lookup for frequently accessed category data.
    /// </summary>
    public class CategoryStore:ICategoryStore
    {
        private readonly Dictionary<int, Category> _categories;

        public CategoryStore( List<Category>? initialCategories = null )
        {
            _categories = new Dictionary<int, Category>();

            if (initialCategories != null)
            {
                // Only cache active categories
                foreach (Category category in initialCategories.Where(c => !c.IsDeleted))
                {
                    _categories[category.CategoryId] = category;
                }
            }
        }

        public void Initialize( IEnumerable<Category> categories )
        {
            _categories.Clear();

            // Only cache active categories
            foreach (Category category in categories.Where(c => !c.IsDeleted))
            {
                _categories[category.CategoryId] = category;
            }
        }

        public void Clear()
        {
            _categories.Clear();
        }

        #region Write Operations

        public CategoryDto? Create( int categoryId, CreateCategoryDto createDto )
        {
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return null;
            }

            // Validate parent exists and is active
            if (createDto.ParentCategoryId.HasValue && !Exists(createDto.ParentCategoryId.Value))
            {
                return null;
            }

            Category category = new(
                categoryId,
                createDto.Name.Trim(),
                createDto.Description?.Trim(),
                createDto.ParentCategoryId,
                createDto.ImageUrl?.Trim(),
                false,
                null
            );

            _categories[categoryId] = category;
            return category.ToDto();
        }

        public CategoryDto? Update( UpdateCategoryDto updateDto )
        {
            // Only update active categories
            if (!_categories.TryGetValue(updateDto.CategoryId, out Category? existingCategory))
            {
                return null; // Category not found in active cache
            }

            if (string.IsNullOrWhiteSpace(updateDto.Name))
            {
                return null;
            }

            // Validate parent exists and is active
            if (updateDto.ParentCategoryId.HasValue)
            {
                if (!Exists(updateDto.ParentCategoryId.Value))
                {
                    return null;
                }

                if (WouldCreateCircularReference(updateDto.CategoryId, updateDto.ParentCategoryId.Value))
                {
                    return null;
                }
            }

            Category updatedCategory = existingCategory with
            {
                Name = updateDto.Name.Trim(),
                Description = updateDto.Description?.Trim(),
                ParentCategoryId = updateDto.ParentCategoryId,
                ImageUrl = updateDto.ImageUrl?.Trim()
            };

            _categories[updateDto.CategoryId] = updatedCategory;
            return updatedCategory.ToDto();
        }

        public bool Delete( int categoryId ) =>
            // Remove from active cache (soft delete removes from cache, hard delete calls this too)
            _categories.Remove(categoryId);

        #endregion

        #region Read Operations

        public CategoryDto? GetById( int categoryId ) =>
            // Only searches active categories
            _categories.TryGetValue(categoryId, out Category? category)
                ? category.ToDto()
                : null;

        public List<CategoryDto> GetAll(
            int? parentId = null,
            string? search = null,
            int skip = 0,
            int take = 100 )
        {
            IEnumerable<Category> categories = _categories.Values;

            if (parentId.HasValue)
            {
                categories = categories.Where(c => c.ParentCategoryId == parentId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLowerInvariant();
                categories = categories.Where(c =>
                                                  c
                                                      .Name.ToLowerInvariant()
                                                      .Contains(searchLower) ||
                                                  c.Description != null && c
                                                                           .Description.ToLowerInvariant()
                                                                           .Contains(searchLower));
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
            return _categories
                   .Values
                   .Where(c => c.ParentCategoryId == null)
                   .OrderBy(c => c.Name)
                   .Select(c => c.ToDto())
                   .ToList();
        }

        public IEnumerable<CategoryDto> GetActiveCategories()
        {
            return _categories
                   .Values
                   .OrderBy(c => c.Name)
                   .Select(p => p.ToDto())
                   .ToList();
        }

        #endregion

        public bool Exists( int categoryId ) =>
            // Only checks active categories
            _categories.ContainsKey(categoryId);

        public int GetCount( int? parentId = null )
        {
            return !parentId.HasValue
                ? _categories.Count
                : _categories.Values.Count(c => c.ParentCategoryId == parentId.Value);

        }

        public int GetActiveCount() => _categories.Count;

        public bool HasChildren( int categoryId )
        {
            return _categories.Values.Any(c => c.ParentCategoryId == categoryId);
        }

        public IEnumerable<Category> SearchCategories( string? searchTerm = null )
        {
            IEnumerable<Category> query = _categories.Values;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                        (c.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return query
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
                if (category?.ParentCategoryId == null)
                    break;

                currentId = category.ParentCategoryId.Value;
            }

            return false;
        }
    }
}
