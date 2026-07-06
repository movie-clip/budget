using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using HomeCharts.Presentation.Mvp;

namespace HomeCharts.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
        : this(new CompositionRoot().CreateMainWindowViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Opened += OnOpened;
        KeyDown += OnKeyDown;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        await _viewModel.LoadInitialStateAsync();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.I)
        {
            ExecuteCommand(_viewModel.ImportCommand);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.R)
        {
            ExecuteCommand(_viewModel.ManualRecategorizeCommand);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F5)
        {
            ExecuteCommand(_viewModel.RefreshCommand);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.F)
        {
            if (this.FindControl<TextBox>("SearchTextBox") is { } searchBox)
            {
                searchBox.Focus();
                searchBox.SelectAll();
            }

            e.Handled = true;
        }
    }

    private static void ExecuteCommand(System.Windows.Input.ICommand command)
    {
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private async void OnBrowseImportFileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (StorageProvider is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select bank statement file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Text files") { Patterns = ["*.txt"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
            ]
        });

        var selectedFile = files.FirstOrDefault();
        if (selectedFile is null)
        {
            return;
        }

        var localPath = selectedFile.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await _viewModel.ImportFromSelectedFileAsync(localPath);
        }
    }

    private async void OnBrowseTransactionsFileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (StorageProvider is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select bank statement file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Text files") { Patterns = ["*.txt"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
            ]
        });

        var selectedFile = files.FirstOrDefault();
        if (selectedFile is null)
        {
            return;
        }

        var localPath = selectedFile.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await _viewModel.PreviewMatchedTransactionsFromSelectedFileAsync(localPath);
        }
    }

    private async void OnClearAllRulesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var confirmed = await ShowConfirmationAsync(
            "Delete all rules?",
            "This will permanently remove all categorization rules.");

        if (!confirmed)
        {
            return;
        }

        ExecuteCommand(_viewModel.ClearAllRulesCommand);
    }

    private async void OnClearAllTransactionsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var confirmed = await ShowConfirmationAsync(
            "Delete all transactions?",
            "This will permanently remove all transactions, manual overrides, and import history.");

        if (!confirmed)
        {
            return;
        }

        ExecuteCommand(_viewModel.ClearAllTransactionsCommand);
    }

    private async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = false;

        var confirmButton = new Button
        {
            Content = "Delete",
            Background = Avalonia.Media.Brush.Parse("#7F1D1D"),
            Foreground = Avalonia.Media.Brushes.White,
            BorderBrush = Avalonia.Media.Brush.Parse("#B91C1C"),
            MinWidth = 100
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Background = Avalonia.Media.Brush.Parse("#0F172A"),
            Foreground = Avalonia.Media.Brush.Parse("#E2E8F0"),
            BorderBrush = Avalonia.Media.Brush.Parse("#334155"),
            MinWidth = 100
        };

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(16),
                Background = Avalonia.Media.Brush.Parse("#0F172A"),
                Child = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Foreground = Avalonia.Media.Brush.Parse("#E2E8F0")
                        },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10,
                            Children = { cancelButton, confirmButton }
                        }
                    }
                }
            }
        };

        confirmButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(this);
        return result;
    }
}
