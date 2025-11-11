using MvvmCross.ViewModels;
using Storix.Application.DTO.Categories;

namespace Storix.Core.ViewModels.Categories
{
    public class CategoryListItemViewModel( CategoryListDto categoryDto ):MvxNotifyPropertyChanged
    {
        private readonly CategoryListDto _category = categoryDto ?? throw new ArgumentNullException(nameof(categoryDto));
        private bool _isSelected;

        #region Category Properties

        public int CategoryId => _category.CategoryId;
        public string Name => _category.Name;
        public string? Description => _category.Description;

        public string? ParentCategory => _category.ParentCategory;
        public string? ImageUrl => _category.ImageUrl;

        #endregion

        #region UI_Specific Properties

        /// <summary>
        /// Indicates whether this product is currently selected in the UI.
        /// Used for checkbox binding in DataGrid.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the underlying Product entity (useful for edit/delete operations).
        /// </summary>
        public CategoryListDto GetCategory() => _category;

        /// <summary>
        /// Resets the selection state.
        /// </summary>
        public void ClearSelection()
        {
            IsSelected = false;
        }

        #endregion
    }
}
