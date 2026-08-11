using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PhotoViewer.Controls
{
    /// <summary>
    /// A virtualizing justified gallery panel.
    /// Only creates UI containers for visible items.
    /// </summary>
    public class JustifiedWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private ScrollViewer _scrollOwner;
        private readonly List<RowLayout> _rows = new();
        private System.Windows.Size _extent = new System.Windows.Size(0, 0);
        private double _verticalOffset;
        private int _itemCount;
        private ItemCollection _itemCollection;

        private ItemCollection ItemCollection
        {
            get
            {
                if (_itemCollection == null)
                {
                    var itemsControl = ItemsControl.GetItemsOwner(this);
                    _itemCollection = itemsControl?.Items;
                }
                return _itemCollection;
            }
        }

        private int ItemCount => ItemCollection?.Count ?? 0;
        private object GetItem(int index) => ItemCollection != null && index >= 0 && index < ItemCollection.Count ? ItemCollection[index] : null;

        public double TargetRowHeight
        {
            get => (double)GetValue(TargetRowHeightProperty);
            set => SetValue(TargetRowHeightProperty, value);
        }

        public static readonly DependencyProperty TargetRowHeightProperty =
            DependencyProperty.Register(nameof(TargetRowHeight), typeof(double), typeof(JustifiedWrapPanel),
                new FrameworkPropertyMetadata(180.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double ItemSpacing
        {
            get => (double)GetValue(ItemSpacingProperty);
            set => SetValue(ItemSpacingProperty, value);
        }

        public static readonly DependencyProperty ItemSpacingProperty =
            DependencyProperty.Register(nameof(ItemSpacing), typeof(double), typeof(JustifiedWrapPanel),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public ScrollViewer ScrollOwner { get => _scrollOwner; set => _scrollOwner = value; }
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public double HorizontalOffset => 0;
        public double VerticalOffset => _verticalOffset;
        public double ExtentHeight => _extent.Height;
        public double ExtentWidth => _extent.Width;
        public double ViewportHeight => _scrollOwner?.ViewportHeight ?? 0;
        public double ViewportWidth => _scrollOwner?.ViewportWidth ?? 0;

        public void SetVerticalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(offset, ExtentHeight - ViewportHeight));
            if (Math.Abs(_verticalOffset - offset) > 0.1)
            {
                _verticalOffset = offset;
                _scrollOwner?.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public void LineUp() => SetVerticalOffset(_verticalOffset - 20);
        public void LineDown() => SetVerticalOffset(_verticalOffset + 20);
        public void PageUp() => SetVerticalOffset(_verticalOffset - ViewportHeight);
        public void PageDown() => SetVerticalOffset(_verticalOffset + ViewportHeight);
        public void MouseWheelUp() => SetVerticalOffset(_verticalOffset - 40);
        public void MouseWheelDown() => SetVerticalOffset(_verticalOffset + 40);
        public void SetHorizontalOffset(double offset) { }
        public void LineLeft() { }
        public void LineRight() { }
        public void PageLeft() { }
        public void PageRight() { }
        public void MouseWheelLeft() { }
        public void MouseWheelRight() { }
        public Rect MakeVisible(System.Windows.Media.Visual visual, Rect rectangle) => rectangle;

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            double width = availableSize.Width;
            if (double.IsInfinity(width) || width <= 0) width = 800;

            int count = ItemCount;
            if (count == 0)
            {
                _rows.Clear();
                _extent = new System.Windows.Size(0, 0);
                _itemCount = 0;
                return new System.Windows.Size(0, 0);
            }

            if (_itemCount != count || _rows.Count == 0 || Math.Abs(width - _extent.Width) > 1)
            {
                ComputeLayout(width, count);
                _itemCount = count;
            }

            double topVisible = _verticalOffset - TargetRowHeight;
            double bottomVisible = _verticalOffset + ViewportHeight + TargetRowHeight;

            int firstIdx = int.MaxValue, lastIdx = 0;
            foreach (var row in _rows)
            {
                if (row.Bottom >= topVisible && row.Top <= bottomVisible)
                {
                    if (row.FirstIndex < firstIdx) firstIdx = row.FirstIndex;
                    if (row.LastIndex > lastIdx) lastIdx = row.LastIndex;
                }
            }

            if (firstIdx == int.MaxValue) { firstIdx = 0; lastIdx = 0; }

            int buffer = 20;
            int start = Math.Max(0, firstIdx - buffer);
            int end = Math.Min(count - 1, lastIdx + buffer);

            var generator = ItemContainerGenerator;
            generator.StartAt(new GeneratorPosition(start, 0), GeneratorDirection.Forward, true);

            for (int i = InternalChildren.Count - 1; i >= 0; i--)
            {
                if (i < start || i > end)
                {
                    var child = InternalChildren[i];
                    var pos = new GeneratorPosition(i, 0);
                    generator.Remove(pos, 1);
                    RemoveVisualChild(child);
                    RemoveLogicalChild(child);
                }
            }

            for (int i = start; i <= end; i++)
            {
                bool isNew;
                var child = generator.GenerateNext(out isNew) as System.Windows.UIElement;
                if (isNew && child != null)
                {
                    InsertInternalChild(i, child);
                    if (child is FrameworkElement fe)
                        fe.DataContext = GetItem(i);
                }
                if (child != null)
                {
                    double w = _itemRects.TryGetValue(i, out var r) ? r.Width : TargetRowHeight * 1.33;
                    double h = _itemRects.TryGetValue(i, out r) ? r.Height : TargetRowHeight;
                    child.Measure(new System.Windows.Size(w, h));
                }
            }

            return new System.Windows.Size(width, _extent.Height);
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            int count = ItemCount;
            if (count == 0) return finalSize;

            if (_itemCount != count || _rows.Count == 0)
            {
                ComputeLayout(finalSize.Width, count);
                _itemCount = count;
            }

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                if (child != null && _itemRects.TryGetValue(i, out var rect))
                {
                    child.Arrange(new Rect(rect.X, rect.Y - _verticalOffset, rect.Width, rect.Height));
                }
            }

            return finalSize;
        }

        private readonly Dictionary<int, Rect> _itemRects = new();

        private void ComputeLayout(double containerWidth, int itemCount)
        {
            _rows.Clear();
            _itemRects.Clear();

            var currentIndices = new List<int>();
            var currentAspects = new List<double>();
            double currentWidth = 0;
            double y = 0;

            for (int i = 0; i < itemCount; i++)
            {
                double ar = GetAspectRatio(i);
                double w = TargetRowHeight * ar;
                double spacing = currentIndices.Count > 0 ? ItemSpacing : 0;

                if (currentWidth + w + spacing > containerWidth && currentIndices.Count > 0)
                {
                    FinishRow(currentIndices, currentAspects, containerWidth, y);
                    y = _rows.Count > 0 ? _rows[_rows.Count - 1].Bottom + ItemSpacing : 0;
                    currentIndices = new List<int>();
                    currentAspects = new List<double>();
                    currentWidth = 0;
                }

                currentIndices.Add(i);
                currentAspects.Add(ar);
                currentWidth += w + (currentIndices.Count > 1 ? ItemSpacing : 0);
            }

            if (currentIndices.Count > 0)
            {
                double x = 0;
                foreach (var idx in currentIndices)
                {
                    double w = TargetRowHeight * GetAspectRatio(idx);
                    _itemRects[idx] = new Rect(x, y, w, TargetRowHeight);
                    x += w + ItemSpacing;
                }
                _rows.Add(new RowLayout { FirstIndex = currentIndices[0], LastIndex = currentIndices[currentIndices.Count - 1], Top = y, Bottom = y + TargetRowHeight });
            }

            _extent = new System.Windows.Size(containerWidth, _rows.Count > 0 ? _rows[_rows.Count - 1].Bottom : 0);
        }

        private void FinishRow(List<int> indices, List<double> aspects, double containerWidth, double y)
        {
            double naturalWidth = 0;
            foreach (var a in aspects) naturalWidth += TargetRowHeight * a;
            naturalWidth += ItemSpacing * Math.Max(0, indices.Count - 1);

            double scale = naturalWidth > 0 ? containerWidth / naturalWidth : 1.0;
            scale = Math.Max(0.5, Math.Min(1.5, scale));
            double rowHeight = TargetRowHeight * scale;

            double x = 0;
            for (int i = 0; i < indices.Count; i++)
            {
                double w = TargetRowHeight * aspects[i] * scale;
                _itemRects[indices[i]] = new Rect(x, y, w, rowHeight);
                x += w + ItemSpacing;
            }

            _rows.Add(new RowLayout { FirstIndex = indices[0], LastIndex = indices[indices.Count - 1], Top = y, Bottom = y + rowHeight });
        }

        private double GetAspectRatio(int index)
        {
            var item = GetItem(index);
            if (item is ViewModels.PhotoItemViewModel vm && vm.Photo != null && vm.Photo.PixelWidth > 0 && vm.Photo.PixelHeight > 0)
                return vm.Photo.AspectRatio;
            return 4.0 / 3.0;
        }

        private class RowLayout
        {
            public int FirstIndex, LastIndex;
            public double Top, Bottom;
        }
    }
}
