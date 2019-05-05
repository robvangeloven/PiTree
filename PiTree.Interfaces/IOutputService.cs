using System.Threading.Tasks;

namespace PiTree.Shared
{
    public interface IOutputService
    {
        Task Start();

        Task Stop();

        Task SignalNewStatus(MonitorStatus status);
    }
}
