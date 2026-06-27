using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Tag.App.ViewModels;

namespace Tag.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a file to order",
            AllowMultiple = false
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
            await Vm.OpenAsync(path);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (Vm is null) return;
        if (!e.DataTransfer.Formats.Contains(DataFormat.File)) return;

        var path = e.DataTransfer.TryGetFile()?.TryGetLocalPath();
        if (path is not null)
            await Vm.OpenAsync(path);
    }
}
