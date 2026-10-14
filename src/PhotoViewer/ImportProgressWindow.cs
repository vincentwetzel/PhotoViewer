using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace PhotoViewer;

internal sealed class ImportProgressWindow : Window, IDisposable
{
    private readonly System.Windows.Controls.ProgressBar _progressBar;
    private readonly TextBlock _statusText;
    private readonly System.Windows.Controls.Button _cancelButton;
    private readonly CancellationTokenSource _cancellation = new();

    public CancellationToken CancellationToken => _cancellation.Token;

    public ImportProgressWindow(int total)
    {
        Title = "Importing photos";
        AutomationProperties.SetName(this, "PhotoViewer import progress");
        Width = 420;
        Height = 150;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (System.Windows.Media.Brush)Application.Current.Resources["WindowBackground"];

        _progressBar = new System.Windows.Controls.ProgressBar { Minimum = 0, Maximum = Math.Max(1, total), Height = 18 };
        AutomationProperties.SetName(_progressBar, "Import progress");
        _statusText = new TextBlock { Text = "Preparing import...", Margin = new Thickness(0, 0, 0, 12), TextTrimming = TextTrimming.CharacterEllipsis };
        _cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        AutomationProperties.SetName(_cancelButton, "Cancel photo import");
        _cancelButton.Click += (_, _) =>
        {
            _cancelButton.Content = "Canceling...";
            _cancelButton.IsEnabled = false;
            _statusText.Text = "Canceling import...";
            _cancellation.Cancel();
        };

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(_statusText);
        panel.Children.Add(_progressBar);
        panel.Children.Add(_cancelButton);
        Content = panel;
        Closed += (_, _) => _cancellation.Cancel();
    }

    public void UpdateProgress(int completed, string fileName)
    {
        _progressBar.Value = completed;
        _statusText.Text = $"Copying {completed} of {_progressBar.Maximum}: {fileName}";
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }
}
