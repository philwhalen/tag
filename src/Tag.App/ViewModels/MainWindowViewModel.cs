using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tag.Core;
using Tag.Core.IO;
using Tag.Core.Model;

namespace Tag.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TagProcessor _processor = new();
    private ParsedDocument? _doc;

    private const long SoftCapBytes = 50L * 1024 * 1024; // ~50 MB soft cap

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isFileLoaded;

    [ObservableProperty]
    private bool _removeTags = true; // default: Remove tags

    [ObservableProperty]
    private string _preview = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Open a file to begin.";

    public ObservableCollection<string> Warnings { get; } = new();

    public bool HasWarnings => Warnings.Count > 0;

    partial void OnRemoveTagsChanged(bool value) => RefreshPreview();

    public async Task OpenAsync(string path)
    {
        try
        {
            StatusMessage = "Loading…";

            var info = new FileInfo(path);
            bool large = info.Exists && info.Length > SoftCapBytes;

            var doc = await Task.Run(() => _processor.Load(path));

            _doc = doc;
            FilePath = path;
            IsFileLoaded = true;

            Warnings.Clear();
            foreach (var w in doc.Warnings)
                Warnings.Add($"Line {w.Line}, Col {w.Column}: {w.Reason}  ⟨{w.Snippet}⟩");
            OnPropertyChanged(nameof(HasWarnings));

            RefreshPreview();

            string warnNote = doc.Warnings.Count > 0 ? $"  ({doc.Warnings.Count} warning(s))" : "";
            string bigNote = large ? "  — large file, preview/save may be slow" : "";
            StatusMessage = $"Loaded {Path.GetFileName(path)}{warnNote}{bigNote}";
        }
        catch (EncodingDetectionException ex)
        {
            ResetLoad();
            StatusMessage = "Cannot open: " + ex.Message;
        }
        catch (Exception ex)
        {
            ResetLoad();
            StatusMessage = "Cannot open: " + ex.Message;
        }
    }

    private void ResetLoad()
    {
        _doc = null;
        FilePath = null;
        IsFileLoaded = false;
        Preview = string.Empty;
        Warnings.Clear();
        OnPropertyChanged(nameof(HasWarnings));
    }

    private void RefreshPreview()
    {
        Preview = _doc is null ? string.Empty : _processor.Render(_doc, RemoveTags);
    }

    private bool CanSave() => IsFileLoaded;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_doc is null || FilePath is null) return;
        try
        {
            StatusMessage = "Saving…";
            string rendered = _processor.Render(_doc, RemoveTags);
            string outPath = await Task.Run(() => _processor.Save(FilePath!, rendered, _doc!));
            StatusMessage = "Saved: " + outPath;
        }
        catch (Exception ex)
        {
            StatusMessage = "Cannot save: " + ex.Message;
        }
    }
}
