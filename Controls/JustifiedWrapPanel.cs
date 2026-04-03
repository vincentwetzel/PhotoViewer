using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PhotoViewer.Controls
{
    /// <summary>
    /// A justified gallery panel that arranges items in rows with a target height,
    /// scaling each row to flush-fill the available width (like Google Photos).
    /// The last row is left-aligned at the target height without stretching.
    /// </summary>
    public class JustifiedWrapPanel : System.Windows.Controls.Panel
    {
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

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            if (InternalChildren.Count == 0) return new System.Windows.Size(0, 0);

            double containerWidth = availableSize.Width;
            if (double.IsInfinity(containerWidth) || containerWidth <= 0)
                containerWidth = 800; // Default fallback

            var rows = BuildRows(containerWidth);
            double totalHeight = 0;

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                double naturalRowWidth = 0;
                foreach (var item in row)
                {
                    naturalRowWidth += item.Width;
                }
                naturalRowWidth += ItemSpacing * Math.Max(0, row.Count - 1);

                // Justify all rows except the last one
                bool isLastRow = (r == rows.Count - 1);
                double rowHeight;

                if (isLastRow)
                {
                    // Last row: keep target height, don't stretch
                    rowHeight = TargetRowHeight;
                }
                else
                {
                    // Scale row height to fill available width
                    double scale = naturalRowWidth > 0 ? containerWidth / naturalRowWidth : 1.0;
                    // Clamp scale to prevent extreme distortion (0.5x to 1.5x)
                    scale = Math.Max(0.5, Math.Min(1.5, scale));
                    rowHeight = TargetRowHeight * scale;
                }

                totalHeight += rowHeight + ItemSpacing;
            }

            return new System.Windows.Size(containerWidth, Math.Max(10, totalHeight));
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            if (InternalChildren.Count == 0) return finalSize;

            double containerWidth = finalSize.Width;
            if (double.IsInfinity(containerWidth) || containerWidth <= 0)
                containerWidth = 800;

            var rows = BuildRows(containerWidth);
            double currentY = 0;

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                double naturalRowWidth = 0;
                foreach (var item in row)
                {
                    naturalRowWidth += item.Width;
                }
                naturalRowWidth += ItemSpacing * Math.Max(0, row.Count - 1);

                bool isLastRow = (r == rows.Count - 1);
                double rowHeight;
                double scale;

                if (isLastRow)
                {
                    rowHeight = TargetRowHeight;
                    scale = 1.0;
                }
                else
                {
                    scale = naturalRowWidth > 0 ? containerWidth / naturalRowWidth : 1.0;
                    scale = Math.Max(0.5, Math.Min(1.5, scale));
                    rowHeight = TargetRowHeight * scale;
                }

                double x = 0;
                for (int i = 0; i < row.Count; i++)
                {
                    var item = row[i];
                    double w = item.Width * scale;
                    double h = rowHeight;

                    var child = InternalChildren[item.Index];
                    child.Arrange(new System.Windows.Rect(x, currentY, w, h));
                    x += w + ItemSpacing;
                }

                currentY += rowHeight + ItemSpacing;
            }

            return finalSize;
        }

        private class LayoutItem
        {
            public int Index { get; set; }
            public double Width { get; set; }
            public double AspectRatio { get; set; }
        }

        private List<List<LayoutItem>> BuildRows(double containerWidth)
        {
            var rows = new List<List<LayoutItem>>();
            var currentRow = new List<LayoutItem>();
            double currentRowWidth = 0;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                double aspectRatio = GetItemAspectRatio(child);
                double itemWidth = TargetRowHeight * aspectRatio;

                double spacing = currentRow.Count > 0 ? ItemSpacing : 0;
                double neededWidth = itemWidth + spacing;

                // If adding this item would exceed container width and row has items, start new row
                if (currentRowWidth + neededWidth > containerWidth && currentRow.Count > 0)
                {
                    rows.Add(new List<LayoutItem>(currentRow));
                    currentRow.Clear();
                    currentRowWidth = 0;
                    spacing = 0;
                }

                currentRow.Add(new LayoutItem { Index = i, Width = itemWidth, AspectRatio = aspectRatio });
                currentRowWidth += itemWidth + spacing;
            }

            // Add remaining items
            if (currentRow.Count > 0)
            {
                rows.Add(currentRow);
            }

            return rows;
        }

        private double GetItemAspectRatio(System.Windows.UIElement child)
        {
            // Try to get aspect ratio from the PhotoItemViewModel in the DataContext
            if (child is System.Windows.FrameworkElement fe &&
                fe.DataContext is PhotoViewer.ViewModels.PhotoItemViewModel vm &&
                vm.Photo != null &&
                vm.Photo.PixelWidth > 0 && vm.Photo.PixelHeight > 0)
            {
                return vm.Photo.AspectRatio;
            }

            // Fallback: use DesiredSize if available
            if (child.DesiredSize.Width > 0 && child.DesiredSize.Height > 0)
            {
                return child.DesiredSize.Width / child.DesiredSize.Height;
            }

            return 4.0 / 3.0; // Default landscape aspect ratio
        }
    }
}
