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
        public int PhotoCount { get; set; }

        public FolderNode(string fullPath, FolderNode? parent = null)
        {
            FullName = fullPath;
            Name = Path.GetFileName(fullPath);
            Parent = parent;
            if (parent != null)
                RootSource = parent.RootSource;
            PhotoCount = 0;
            HasSubFolders = CheckHasSubFolders();
        }

        /// <summary>
        /// Quick check if folder has subdirectories — stops at first match (doesn't enumerate all).
        /// </summary>
        private bool CheckHasSubFolders()
        {
            try
            {
                return Directory.EnumerateDirectories(FullName).Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads immediate subfolders (shallow) to enable expand/collapse.
        /// Only called when user expands a node — not during construction.
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
        /// Photo count is calculated asynchronously.
        /// </summary>
        public void LoadSubFolders()
        {
            SubFolders.Clear();
            LoadSubFolderShallow();
            // PhotoCount is not set here - it's calculated asynchronously by the parent
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
            // PhotoCount not updated here - async calculation only
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
        /// Uses yield return for lazy enumeration to avoid loading all paths into memory at once.
        /// </summary>
        public IEnumerable<string> GetAllPhotoPaths()
        {
            // Enumerate photos in this folder
            var files = GetPhotoFilesInCurrentFolder();
            foreach (var file in files)
            {
                yield return file;
            }

            // Recurse into subfolders
            foreach (var sub in SubFolders)
            {
                foreach (var path in sub.GetAllPhotoPaths())
                {
                    yield return path;
                }
            }
        }

        private IEnumerable<string> GetPhotoFilesInCurrentFolder()
        {
            try
            {
                return Directory.EnumerateFiles(FullName, "*.*")
                    .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
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
