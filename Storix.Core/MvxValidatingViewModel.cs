using System.Collections;
using System.ComponentModel;
using MvvmCross.ViewModels;

namespace Storix.Core
{
    public class MvxValidatingViewModel:MvxNotifyPropertyChanged, INotifyDataErrorInfo
    {
        protected readonly Dictionary<string, List<string>> _errors = new();

        public bool IsValid => !HasErrors;
        public bool HasErrors => _errors.Count != 0;

        public IEnumerable GetErrors( string propertyName ) => _errors.ContainsKey(propertyName) ? _errors[propertyName] : Enumerable.Empty<string>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public string GetErrorsAsString( string propertyName )
        {
            IEnumerable<string> errors = GetErrors(propertyName).Cast<string>();
            return string.Join(Environment.NewLine, errors);
        }

        protected void AddError( string propertyName, string errorMessage )
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            _errors[propertyName].Add(errorMessage);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected void AddErrors( string propertyName, IEnumerable<string> errorMessages )
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            _errors[propertyName].AddRange(errorMessages);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected void ClearErrors( string propertyName )
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        protected void ClearAllErrors()
        {
            List<string> propertyNames = _errors.Keys.ToList();
            _errors.Clear();

            foreach (string propertyName in propertyNames)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }

            RaisePropertyChanged(() => IsValid);
            RaisePropertyChanged(() => HasErrors);
        }
    }
}
