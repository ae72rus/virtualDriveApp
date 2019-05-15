using DemoApp.Abstractions.Viewmodels;
using OrlemSoftware.Basics.Core;

namespace DemoApp.Implementations.Factories
{
    public interface ISearchResultViewModelFactory : IFactory
    {
        ISearchResultViewModel Create(IFileSystemViewModel fileSystemViewModel, string searchPattern);
    }
}