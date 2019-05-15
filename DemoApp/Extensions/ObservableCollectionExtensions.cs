using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DemoApp.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void InsertAuto<T, TMember>(this ObservableCollection<T> collection, T item, Func<T, TMember> sortingValueAccessor)
            where TMember : IComparable
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (sortingValueAccessor == null)
                throw new ArgumentNullException(nameof(sortingValueAccessor));

            var itemSortingValue = sortingValueAccessor(item);

            //if last < item
            if (!collection.Any() 
                || sortingValueAccessor(collection[collection.Count - 1]) == null 
                || sortingValueAccessor(collection[collection.Count - 1]).CompareTo(itemSortingValue) < 0)
            {
                collection.Add(item);
                return;
            }

            //if first > item
            if (sortingValueAccessor(collection[0]).CompareTo(itemSortingValue) > 0)
            {
                collection.Insert(0, item);
                return;
            }

            for (var i = 1; i < collection.Count - 1; i++)//first and last elements are already checked
            {
                var colItem = collection[i];
                if (sortingValueAccessor(colItem).CompareTo(itemSortingValue) < 0)
                    continue;

                collection.Insert(i, item);
                return;
            }
            
            collection.Add(item);
        }
    }
}