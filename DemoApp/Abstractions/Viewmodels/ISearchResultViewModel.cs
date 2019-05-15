using DemoApp.Abstractions.Common;

namespace DemoApp.Abstractions.Viewmodels
{
    public interface ISearchResultViewModel : IViewModel
    {
        string SearchPattern { get; }
        void AddResult(IEntityInfo result);
    }
}