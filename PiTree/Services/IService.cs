using System.Threading.Tasks;

namespace PiTree.Services
{
    internal interface IService
    {
        Task Start();
        Task Stop();
    }
}
