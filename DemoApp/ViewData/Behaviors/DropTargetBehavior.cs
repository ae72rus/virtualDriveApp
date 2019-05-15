using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace DemoApp.ViewData.Behaviors
{
    public class DropTargetBehavior : Behavior<ListView>
    {
        public static DependencyProperty FileDropCommandProperty = DependencyProperty.Register(nameof(FileDropCommand), typeof(ICommand), typeof(DropTargetBehavior));
        public static DependencyProperty ItemsDropCommandProperty = DependencyProperty.Register(nameof(ItemsDropCommand), typeof(ICommand), typeof(DropTargetBehavior));

        public ICommand FileDropCommand
        {
            get => (ICommand)GetValue(FileDropCommandProperty);
            set => SetValue(FileDropCommandProperty, value);
        }

        public ICommand ItemsDropCommand
        {
            get => (ICommand)GetValue(ItemsDropCommandProperty);
            set => SetValue(ItemsDropCommandProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.Drop += onDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Drop -= onDrop;
        }

        private void onDrop(object sender, DragEventArgs e)
        {
            var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            var items = (IList)e.Data.GetData(typeof(IList));

            onFilesDrop(fileNames);
            onItemsDrop(items);
        }

        private void onFilesDrop(string[] fileNames)
        {
            if(fileNames == null)
                return;

            if (FileDropCommand?.CanExecute(fileNames) == true)
                FileDropCommand.Execute(fileNames);
        }

        private void onItemsDrop(IList items)
        {
            if (items == null)
                return;

            if (ItemsDropCommand?.CanExecute(items) == true)
                ItemsDropCommand.Execute(items);
        }
    }
}