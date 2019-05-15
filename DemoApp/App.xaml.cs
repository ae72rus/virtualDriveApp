using System.Windows;
using DemoApp.Abstractions.Services;
using DemoApp.Implementations.Viewmodels;
using OrlemSoftware.Basics.Core.Implementation;

namespace DemoApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private OrlemContainer _container;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _container = new OrlemContainer(new MockLoggingService());
            _container.AddDependenciesSource<DemoAppDependenciesSource>();
            _container.Start();

            var startWindowVm = _container.Resolve<StartWindowViewModel>();
            var windowsManager = _container.Resolve<IWindowsManager>();
            windowsManager.OpenStartWindow(startWindowVm);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _container.Stop();
            base.OnExit(e);
        }
    }
}
