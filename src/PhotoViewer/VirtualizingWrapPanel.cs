using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PhotoViewer.Controls
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private System.Windows.Size _extent = new System.Windows.Size(0, 0);
        private System.Windows.Size _viewport = new System.Windows.Size(0, 0);
        private System.Windows.Point _offset;

        private ItemsControl? _itemsControl;
        private IItemContainerGenerator? _generator;

        private System.Windows.Size _childSize;
        private int _childrenPerRow;

        private System.Windows.Size CalculateChildSize()
        {
            // For this app, we know the items are fixed size 150x150 + margins.
            return new System.Windows.Size(160, 160); // 150x150 item + 5 margin on each side
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            _itemsControl = ItemsControl.GetItemsOwner(this);
            _generator = _itemsControl?.ItemContainerGenerator;
            _viewport = availableSize;
            _childSize = CalculateChildSize();

            if (_itemsControl == null || _generator == null || _itemsControl.Items.Count == 0 || _childSize.Width == 0)
            {
                return new System.Windows.Size(0, 0);
            }

            _childrenPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / _childSize.Width));
            UpdateExtent();
            RealizeChildren();

            return _viewport;
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            if (_generator == null || InternalChildren.Count == 0)
            {
                return finalSize;
            }

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                int itemIndex = ((ItemContainerGenerator)_generator).IndexFromContainer(child);

                if (itemIndex == -1) continue;

                int row = itemIndex / _childrenPerRow;
                int col = itemIndex % _childrenPerRow;

                Rect childRect = new Rect(col * _childSize.Width, row * _childSize.Height, _childSize.Width, _childSize.Height);
                childRect.Offset(-_offset.X, -_offset.Y);
                child.Arrange(childRect);
            }

            return finalSize;
        }

        private void RealizeChildren()
        {
            if (_generator == null || _itemsControl == null) return;

            var (startIndex, endIndex) = GetVisibleItemsRange();
            if (startIndex > endIndex)
            {
                RemoveInternalChildRange(0, InternalChildren.Count);
                return;
            }

            GeneratorPosition startPos = _generator.GeneratorPositionFromIndex(startIndex);
            int childIndex = _generator.IndexFromGeneratorPosition(startPos);

            using (_generator.StartAt(startPos, GeneratorDirection.Forward, true))
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    bool newlyRealized;
                    var child = _generator.GenerateNext(out newlyRealized) as UIElement;
                    if (child == null) continue;

                    if (newlyRealized)
                    {
                        if (childIndex >= InternalChildren.Count)
                        {
                            AddInternalChild(child);
                        }
                        else
                        {
                            InsertInternalChild(childIndex, child);
                        }
                        _generator.PrepareItemContainer(child);
                    }
                    childIndex++;
                }
            }

            for (int i = InternalChildren.Count - 1; i >= 0; i--)
            {
                var child = InternalChildren[i];
                int itemIndex = ((ItemContainerGenerator)_generator).IndexFromContainer(child);
                if (itemIndex != -1 && (itemIndex < startIndex || itemIndex > endIndex))
                {
                    GeneratorPosition pos = _generator.GeneratorPositionFromIndex(itemIndex);
                    _generator.Remove(pos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
        }

        private (int, int) GetVisibleItemsRange()
        {
            if (_itemsControl == null || _itemsControl.Items.Count == 0 || _childSize.Height == 0)
            {
                return (-1, -1);
            }

            int firstRow = (int)Math.Floor(_offset.Y / _childSize.Height);
            int startIndex = firstRow * _childrenPerRow;

            int visibleRows = (int)Math.Ceiling(_viewport.Height / _childSize.Height) + 1;
            int endIndex = Math.Min(startIndex + visibleRows * _childrenPerRow - 1, _itemsControl.Items.Count - 1);

            return (startIndex, endIndex);
        }

        private void UpdateExtent()
        {
            if (_itemsControl == null || _itemsControl.Items.Count == 0)
            {
                _extent = new System.Windows.Size(0, 0);
            }
            else
            {
                int numRows = (int)Math.Ceiling((double)_itemsControl.Items.Count / _childrenPerRow);
                _extent = new System.Windows.Size(_childrenPerRow * _childSize.Width, numRows * _childSize.Height);
            }

            ScrollOwner?.InvalidateScrollInfo();
        }

        public ScrollViewer? ScrollOwner { get; set; }
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;
        public double ExtentWidth => _extent.Width;
        public double ExtentHeight => _extent.Height;
        public double ViewportWidth => _viewport.Width;
        public double ViewportHeight => _viewport.Height;

        public void LineUp() => SetVerticalOffset(VerticalOffset - 16);
        public void LineDown() => SetVerticalOffset(VerticalOffset + 16);
        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - 16);
        public void LineRight() => SetHorizontalOffset(HorizontalOffset + 16);

        public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
        public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
        public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - 48);
        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + 48);
        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - 48);
        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + 48);

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || ViewportWidth >= ExtentWidth)
            {
                offset = 0;
            }
            else if (offset + ViewportWidth >= ExtentWidth)
            {
                offset = ExtentWidth - ViewportWidth;
            }

            if (offset != _offset.X)
            {
                _offset.X = offset;
                InvalidateMeasure();
            }
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || ViewportHeight >= ExtentHeight)
            {
                offset = 0;
            }
            else if (offset + ViewportHeight >= ExtentHeight)
            {
                offset = ExtentHeight - ViewportHeight;
            }

            if (offset != _offset.Y)
            {
                _offset.Y = offset;
                InvalidateMeasure();
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }
    }
}
