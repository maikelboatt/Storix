using MvvmCross.ViewModels;

namespace Storix.Application.DTO.Categories
{
    /// <summary>
    /// DTO for subcategory information
    /// </summary>
    public class SubcategoryInfo:MvxNotifyPropertyChanged
    {
        private int _categoryId;
        private string? _name;
        private string? _description;
        private int _productCount;

        public int CategoryId
        {
            get => _categoryId;
            set => SetProperty(ref _categoryId, value);
        }

        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int ProductCount
        {
            get => _productCount;
            set => SetProperty(ref _productCount, value);
        }
    }
}
