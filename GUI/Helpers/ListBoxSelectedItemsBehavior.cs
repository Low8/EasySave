using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ListBox = System.Windows.Controls.ListBox;

namespace EasySave.GUI.Helpers
{
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListBoxSelectedItemsBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject element, IList value) =>
            element.SetValue(SelectedItemsProperty, value);

        public static IList GetSelectedItems(DependencyObject element) =>
            (IList)element.GetValue(SelectedItemsProperty);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox)
                return;

            listBox.SelectionChanged -= ListBoxOnSelectionChanged;
            listBox.SelectionChanged += ListBoxOnSelectionChanged;
            listBox.PreviewMouseLeftButtonDown -= ListBoxOnPreviewMouseLeftButtonDown;
            listBox.PreviewMouseLeftButtonDown += ListBoxOnPreviewMouseLeftButtonDown;

            SyncSelectedItems(listBox);
        }

        private static void ListBoxOnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var element = e.OriginalSource as DependencyObject;
            var item = ItemsControl.ContainerFromElement(listBox, element) as ListBoxItem;
            if (item == null)
                return;

            if (item.IsSelected)
            {
                item.IsSelected = false;
                e.Handled = true;
                return;
            }

            item.IsSelected = true;
            e.Handled = true;
        }

        private static void ListBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var boundList = GetSelectedItems(listBox);
            if (boundList == null)
                return;

            foreach (var item in e.RemovedItems.Cast<object>())
                boundList.Remove(item);

            foreach (var item in e.AddedItems.Cast<object>())
                if (!boundList.Contains(item))
                    boundList.Add(item);
        }

        private static void SyncSelectedItems(ListBox listBox)
        {
            var boundList = GetSelectedItems(listBox);
            if (boundList == null)
                return;

            listBox.SelectedItems.Clear();
            foreach (var item in boundList.Cast<object>())
                listBox.SelectedItems.Add(item);
        }
    }
}
