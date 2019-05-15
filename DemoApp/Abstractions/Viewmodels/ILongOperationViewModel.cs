using System.Threading;
using System.Threading.Tasks;
using VirtualDrive;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface ILongOperationViewModel : IViewModel
    {
        Task Task { get; }
        IVirtualFileSystem FileSystem { get; }
        bool IsCanceled { get; }
        int Progress { get; set; }
        string Message { get; set; }
        CancellationTokenSource CancellationTokenSource { get; }
        void SetState(ProgressArgs progressArgs);
        void Cancel();
    }
}