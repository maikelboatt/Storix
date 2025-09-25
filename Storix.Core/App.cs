using MvvmCross.ViewModels;
using Storix.Core.ViewModels;

namespace Storix.Core
{
    public class App:MvxApplication
    {
        public override void Initialize()
        {
            RegisterAppStart<ShellViewModel>();
        }
    }
}
