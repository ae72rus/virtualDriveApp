using DemoApp.Abstractions.Viewmodels;
using OrlemSoftware.Basics.Core;

namespace DemoApp.Implementations.Factories
{
    public interface ISearchViewModelFactory : IFactory
    {
        ISearchViewModel Create();
    }
}