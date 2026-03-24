using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SshManager.Views
{
    /// <summary>
    /// A TabControl that keeps each tab's visual tree alive when switching tabs.
    /// The default WPF TabControl destroys and recreates tab content on every switch,
    /// which breaks stateful controls like WebView2. This version maintains a
    /// ContentPresenter per tab in a Grid, toggling Visibility instead.
    /// </summary>
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class CachedTabControl : TabControl
    {
        private Panel _itemsHolder;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsHolder = GetTemplateChild("PART_ItemsHolder") as Panel;
            EnsureChildren();
            UpdateVisibility();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (_itemsHolder == null) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                        foreach (var item in e.NewItems)
                            AddContentPresenter(item);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (var item in e.OldItems)
                            RemoveChild(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _itemsHolder.Children.Clear();
                    break;
            }

            UpdateVisibility();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateVisibility();
        }

        /// <summary>
        /// Ensure all items have a corresponding ContentPresenter (handles startup / template apply timing).
        /// </summary>
        private void EnsureChildren()
        {
            if (_itemsHolder == null) return;

            foreach (var item in Items)
            {
                if (FindPresenter(item) == null)
                    AddContentPresenter(item);
            }
        }

        private void AddContentPresenter(object item)
        {
            if (_itemsHolder == null) return;

            var cp = new ContentPresenter
            {
                Content = item,
                ContentTemplate = ContentTemplate,
                ContentTemplateSelector = ContentTemplateSelector,
                ContentStringFormat = ContentStringFormat,
                Visibility = Visibility.Collapsed
            };

            _itemsHolder.Children.Add(cp);
        }

        private void RemoveChild(object item)
        {
            if (_itemsHolder == null) return;

            var cp = FindPresenter(item);
            if (cp != null)
                _itemsHolder.Children.Remove(cp);
        }

        private ContentPresenter FindPresenter(object item)
        {
            if (_itemsHolder == null) return null;

            foreach (UIElement child in _itemsHolder.Children)
            {
                if (child is ContentPresenter cp && cp.Content == item)
                    return cp;
            }
            return null;
        }

        private void UpdateVisibility()
        {
            if (_itemsHolder == null) return;

            foreach (UIElement child in _itemsHolder.Children)
            {
                if (child is ContentPresenter cp)
                {
                    cp.Visibility = cp.Content == SelectedItem
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }
    }
}
