
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DemoApp.ViewData.Controls
{
    /// <summary>
    /// Interaction logic for EntityView.xaml
    /// </summary>
    public partial class EntityView : INotifyPropertyChanged
    {
        public static DependencyProperty EntityTypeProperty
            = DependencyProperty.Register(nameof(EntityType), typeof(string), typeof(EntityView));

        public static DependencyProperty EntityNameProperty
            = DependencyProperty.Register(nameof(EntityName), typeof(string), typeof(EntityView));

        public static DependencyProperty EntityEditableNameProperty
            = DependencyProperty.Register(nameof(EntityEditableName), typeof(string), typeof(EntityView), new PropertyMetadata(onEntityEditableNamePropertyChanged));

        public static DependencyProperty IsInRenameModeProperty
            = DependencyProperty.Register(nameof(IsInRenameMode), typeof(bool), typeof(EntityView), new PropertyMetadata(onIsInRenameModePropertyChanged));

        private static void onEntityEditableNamePropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            ((EntityView)sender).onEntityEditableNameChanged();
        }
        private static void onIsInRenameModePropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            ((EntityView)sender).onIsInRenameModeChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string EntityType
        {
            get => (string)GetValue(EntityTypeProperty);
            set => SetValue(EntityTypeProperty, value);
        }

        public string EntityName
        {
            get => (string)GetValue(EntityNameProperty);
            set => SetValue(EntityNameProperty, value);
        }

        public string EntityEditableName
        {
            get => (string)GetValue(EntityEditableNameProperty);
            set => SetValue(EntityEditableNameProperty, value);
        }

        public bool IsInRenameMode
        {
            get => (bool)GetValue(IsInRenameModeProperty);
            set => SetValue(IsInRenameModeProperty, value);
        }

        public EntityView()
        {
            InitializeComponent();
        }

        private void onEntityEditableNameChanged()
        {
            OnPropertyChanged(nameof(EntityEditableName));
        }

        private void onIsInRenameModeChanged()
        {
            OnPropertyChanged(nameof(IsInRenameMode));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void onTextBoxIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (textbox.Visibility != Visibility.Visible)
                return;

            textbox.SelectAll();
            textbox.Focus();
        }

        private void onTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsInRenameMode)
                return;

            if (e.Key != Key.Enter && e.Key != Key.Escape)
                return;

            e.Handled = true;
            if (e.Key == Key.Escape)
                EntityEditableName = EntityName;

            IsInRenameMode = false;
        }
    }
}
