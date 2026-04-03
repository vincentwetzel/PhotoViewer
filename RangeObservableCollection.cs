using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PhotoViewer
{
    /// <summary>
    /// An ObservableCollection that supports AddRange with a single CollectionChanged event,
    /// enabling instant population of large lists without per-item UI updates.
    /// </summary>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification;

        public void AddRange(IEnumerable<T> items)
        {
            _suppressNotification = true;
            foreach (var item in items)
                Items.Add(item);
            _suppressNotification = false;

            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_suppressNotification) return;
            base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_suppressNotification) return;
            base.OnPropertyChanged(e);
        }
    }
}
