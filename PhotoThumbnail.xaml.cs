using System.Windows;
using System.Windows.Controls;

namespace PhotoViewer.Controls
{
    /// <summary>
    /// Interaction logic for PhotoThumbnail.xaml
    /// </summary>
    public partial class PhotoThumbnail : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(PhotoThumbnail), new PropertyMetadata(null));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        public PhotoThumbnail()
        {
            InitializeComponent();
        }
    }
}
