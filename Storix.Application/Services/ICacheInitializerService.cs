using System.Threading.Tasks;

namespace Storix.Application.Services
{
    public interface ICacheInitializerService
    {
        Task InitializeCacheAsync();
    }
}
