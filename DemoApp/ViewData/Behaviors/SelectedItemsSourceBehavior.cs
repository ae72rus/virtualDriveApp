using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace DemoApp.ViewData.Behaviors
{
    public class SelectedItemsSourceBehavior : Behavior<ListView>
    {
        public static DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems),
                                                                                                typeof(IList),
                                                                                                typeof(SelectedItemsSourceBehavior),
                                                                                                new FrameworkPropertyMetadata(selectedItemsPropertyChangedHandler));

        private static void selectedItemsPropertyChangedHandler(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((SelectedItemsSourceBehavior)sender).onSelectedItemsChanged((IList)e.OldValue, (IList)e.NewValue);
        }

        private bool _isSettingFromViewModel;
        private bool _isSettingFromUI;

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.SelectionChanged += onSelectionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= onSelectionChanged;
        }

        private void onSelectedItemsChanged(IList oldValue, IList newValue)
        {
            if (ReferenceEquals(oldValue, newValue))
                return;

            if (oldValue is INotifyCollectionChanged oldObservableCollection)
                oldObservableCollection.CollectionChanged -= onCollectionsChanged;

            if (newValue is INotifyCollectionChanged newObservableCollection)
                newObservableCollection.CollectionChanged += onCollectionsChanged;

            synchronizeToAssociatedObject();
        }

        private void onSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSettingFromViewModel)
                return;

            _isSettingFromUI = true;

            SelectedItems = AssociatedObject.SelectedItems;

            _isSettingFromUI = false;
        }

        private void synchronizeToAssociatedObject()
        {
            if (SelectedItems == null
                || AssociatedObject?.SelectedItems == null
                || _isSettingFromUI
                || ReferenceEquals(SelectedItems, AssociatedObject.SelectedItems))
                return;

            _isSettingFromViewModel = true;

            AssociatedObject.SelectedItems.Clear();
            foreach (var selectedItem in SelectedItems)
                AssociatedObject.SelectedItems.Add(selectedItem);

            _isSettingFromViewModel = false;
        }

        private void onCollectionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            synchronizeToAssociatedObject();
        }
    }
}