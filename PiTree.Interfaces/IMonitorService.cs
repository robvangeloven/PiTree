using System.Threading.Tasks;

namespace PiTree.Shared
{
    public interface IMonitorService
    {
        Task Start();

        Task Stop();
    }
}
