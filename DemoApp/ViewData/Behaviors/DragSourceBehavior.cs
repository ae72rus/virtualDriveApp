using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace DemoApp.ViewData.Behaviors
{
    public class DragSourceBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            AssociatedObject.MouseMove += onMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseMove += onMouseMove;
        }

        private void onMouseMove(object sender, MouseEventArgs e)
        {
            if (AssociatedObject.SelectedItems.Count == 0
                || e.LeftButton != MouseButtonState.Pressed)
                return;

            var data = new DataObject(typeof(IList), AssociatedObject.SelectedItems);
            DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Copy | DragDropEffects.Move);
        }

    }
}