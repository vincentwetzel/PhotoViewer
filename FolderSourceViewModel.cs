using PhotoViewer.Models;
using PhotoViewer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhotoViewer.ViewModels
{
    /// <summary>
    /// Represents a local folder source in the navigation tree.
    /// Exposes a tree of FolderNode items for expand/collapse browsing.
    /// </summary>
    public class FolderSourceViewModel : INotifyPropertyChanged
    {
        public LocalFolderProvider Provider { get; }
        public FolderNode Root { get; }

        public string DisplayName { get; }

        /// <summary>
        /// Total photo count across all folders and subfolders.
        /// </summary>
        public int PhotoCount => Root.TotalPhotoCount;

        private object? _selectedItem;
        /// <summary>
        /// The currently selected item — either a FolderNode or this root itself.
        /// When null or the root, the entire folder tree (all subfolders) is used.
        /// When a FolderNode, that folder and its subfolders are used.
        /// </summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public FolderSourceViewModel(LocalFolderProvider provider)
        {
            Provider = provider;
            // Create root with RootSource set before children are loaded
            Root = new FolderNode(provider.SourceName);
            Root.RootSource = this;
            // Re-load subfolders now that RootSource is set, so children inherit it
            Root.LoadSubFolders();
            DisplayName = System.IO.Path.GetFileName(provider.SourceName);
        }

        /// <summary>
        /// Gets all photo file paths for the currently selected item.
        /// If nothing specific is selected (root level), returns all photos in the tree.
        /// If a FolderNode is selected, returns photos from that folder and all its subfolders.
        /// </summary>
        public IEnumerable<string> GetSelectedPhotoPaths()
        {
            if (_selectedItem is FolderNode node)
            {
                return node.GetAllPhotoPaths();
            }

            // Default: all photos in the entire folder tree
            return Root.GetAllPhotoPaths();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
