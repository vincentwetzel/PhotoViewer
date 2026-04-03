using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PhotoViewer.Models
{
    /// <summary>
    /// Represents a folder in the source tree view. Supports lazy-loading of subfolders.
    /// </summary>
    public class FolderNode : INotifyPropertyChanged
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

        public string FullName { get; }
        public string Name { get; }
        public FolderNode? Parent { get; }

        /// <summary>
        /// Reference to the root FolderSourceViewModel that owns this node tree.
        /// Set by FolderSourceViewModel during construction.
        /// </summary>
        public object? RootSource { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        private bool _hasSubFolders;
        public bool HasSubFolders
        {
            get => _hasSubFolders;
            set { _hasSubFolders = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FolderNode> SubFolders { get; } = new();
        public int PhotoCount { get; private set; }

        /// <summary>
        /// Total photo count including all subfolders recursively.
        /// </summary>
        public int TotalPhotoCount => GetAllPhotoPaths().Count();

        public FolderNode(string fullPath, FolderNode? parent = null)
        {
            FullName = fullPath;
            Name = Path.GetFileName(fullPath);
            Parent = parent;
            if (parent != null)
                RootSource = parent.RootSource;
            PhotoCount = CountPhotosInFolder();
            LoadSubFolderShallow();
        }

        /// <summary>
        /// Loads immediate subfolders (shallow) to enable expand/collapse.
        /// </summary>
        private void LoadSubFolderShallow()
        {
            try
            {
                var dirs = Directory.EnumerateDirectories(FullName);
                foreach (var dir in dirs)
                {
                    SubFolders.Add(new FolderNode(dir, this));
                }
                HasSubFolders = SubFolders.Count > 0;
            }
            catch
            {
                HasSubFolders = false;
            }
        }

        /// <summary>
        /// Public wrapper to reload subfolders. Used by FolderSourceViewModel after RootSource is set.
        /// </summary>
        public void LoadSubFolders()
        {
            SubFolders.Clear();
            LoadSubFolderShallow();
            PhotoCount = CountPhotosInFolder();
        }

        /// <summary>
        /// Re-scans the directory for added/removed subfolders and updates the photo count.
        /// Used to pick up external changes (e.g., folders added/deleted outside the app).
        /// </summary>
        public void RefreshSubFolders()
        {
            // Get current disk subfolders
            var diskFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(FullName))
                    diskFolders.Add(dir);
            }
            catch { }

            // Remove nodes that no longer exist on disk
            var toRemove = SubFolders.Where(n => !diskFolders.Contains(n.FullName)).ToList();
            foreach (var node in toRemove)
                SubFolders.Remove(node);

            // Add new folders that exist on disk but not in our tree
            foreach (var diskDir in diskFolders)
            {
                if (!SubFolders.Any(n => n.FullName.Equals(diskDir, StringComparison.OrdinalIgnoreCase)))
                {
                    var newNode = new FolderNode(diskDir, this);
                    SubFolders.Add(newNode);
                }
            }

            HasSubFolders = SubFolders.Count > 0;
            PhotoCount = CountPhotosInFolder();
        }

        /// <summary>
        /// Recursively refreshes this folder and ALL descendants, not just expanded ones.
        /// Used to pick up external changes at all levels of the tree.
        /// </summary>
        public void RefreshSubFoldersRecursive()
        {
            RefreshSubFolders();
            // Recurse into ALL children, expanded or not — EnumerateDirectories is fast
            foreach (var sub in SubFolders.ToList()) // ToList to allow modification during iteration
            {
                sub.RefreshSubFoldersRecursive();
            }
        }

        /// <summary>
        /// Recursively collects all subfolders (deep) for photo aggregation.
        /// </summary>
        public IEnumerable<string> GetAllFolderPaths()
        {
            var folders = new List<string> { FullName };
            foreach (var sub in SubFolders)
            {
                folders.AddRange(sub.GetAllFolderPaths());
            }
            return folders;
        }

        /// <summary>
        /// Recursively finds all photo file paths in this folder and all subfolders.
        /// </summary>
        public IEnumerable<string> GetAllPhotoPaths()
        {
            var photos = new List<string>();
            try
            {
                var files = Directory.EnumerateFiles(FullName, "*.*")
                    .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
                photos.AddRange(files);
            }
            catch { }

            foreach (var sub in SubFolders)
            {
                photos.AddRange(sub.GetAllPhotoPaths());
            }

            return photos;
        }

        private int CountPhotosInFolder()
        {
            try
            {
                return Directory.EnumerateFiles(FullName, "*.*")
                    .Count(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
            }
            catch
            {
                return 0;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
