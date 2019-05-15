using System;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface ISearchViewModel : IViewModel
    {
        event EventHandler SearchRequest;
        string SearchPattern { get; }
        bool IsRecursive { get; }
    }
}