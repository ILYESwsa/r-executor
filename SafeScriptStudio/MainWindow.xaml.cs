using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SafeScriptStudio;

public partial class MainWindow : Window
{
    private const string TerminalPipeName = "SafeScriptStudioPipe";

    private static readonly string[] ScriptItems =
    [
        "Welcome.lua",
        "MovementChecks.lua",
        "EconomyTests.lua"
    ];

    private static readonly string[] ConfigItems =
    [
        "ui-settings.json",
        "theme.json",
        "rules.yml"
    ];

    private static readonly string[] FileItems =
    [
        "README.md",
        "notes.txt",
        "changelog.txt"
    ];

    private readonly Dictionary<int, ScriptTabState> _tabs = [];
    private readonly Dictionary<int, TabItem> _tabViews = [];
    private int _nextTabId = 1;
    private bool _isUpdatingUi;

    private readonly Brush _activeNavBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2D45"));
    private readonly Brush _activeNavBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FA2FF"));

    private Process? _terminalHostProcess;

    public MainWindow()
    {
        InitializeComponent();

        SetActiveNav(ScriptsButton);
        ReplaceListItems(ScriptItems);

        CreateTabAndSelect("Welcome.lua", "-- local script workspace\nprint(\"Ready\")");
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.N:
                NewScriptButton_Click(NewScriptButton, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.S:
                SaveButton_Click(SaveButton, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.W:
                CloseCurrentTab();
                e.Handled = true;
                break;
        }
    }

    private void ScriptsButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav(ScriptsButton);
        ReplaceListItems(ScriptItems);
        StatusText.Text = "Scripts view loaded.";
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav(ConfigButton);
        ReplaceListItems(ConfigItems);
        StatusText.Text = "Config view loaded.";
    }

    private void FilesButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNav(FilesButton);
        ReplaceListItems(FileItems);
        StatusText.Text = "Files view loaded.";
    }

    private void TxtFilterButton_Click(object sender, RoutedEventArgs e)
    {
        ReplaceListItems(
            "notes.txt",
            "server-rules.txt",
            "deploy-checklist.txt");

        StatusText.Text = "TXT filter applied.";
    }

    private void AllFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (ScriptsButton.Background == _activeNavBackground)
        {
            ReplaceListItems(ScriptItems);
        }
        else if (ConfigButton.Background == _activeNavBackground)
        {
            ReplaceListItems(ConfigItems);
        }
        else
        {
            ReplaceListItems(FileItems);
        }

        StatusText.Text = "All items shown.";
    }

    private void NewScriptButton_Click(object sender, RoutedEventArgs e)
    {
        var tabName = $"Script-{DateTime.Now:HHmmss}.lua";
        CreateTabAndSelect(tabName, "-- new local script\n");
        StatusText.Text = "Created a new script tab.";
    }

    private void ScriptTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || ScriptTabs.SelectedItem is not TabItem selected || selected.Tag is not int tabId)
        {
            return;
        }

        if (!_tabs.TryGetValue(tabId, out var tab))
        {
            return;
        }

        _isUpdatingUi = true;
        ScriptNameTextBox.Text = tab.Name;
        EditorTextBox.Text = tab.Content;
        _isUpdatingUi = false;
    }

    private void ScriptNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingUi || ScriptTabs.SelectedItem is not TabItem selected || selected.Tag is not int tabId)
        {
            return;
        }

        if (!_tabs.TryGetValue(tabId, out var tab))
        {
            return;
        }

        tab.Name = ScriptNameTextBox.Text;
        SetTabHeader(selected, tab.Name, tabId, tab.IsDirty);
    }

    private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingUi || ScriptTabs.SelectedItem is not TabItem selected || selected.Tag is not int tabId)
        {
            return;
        }

        if (!_tabs.TryGetValue(tabId, out var tab))
        {
            return;
        }

        tab.Content = EditorTextBox.Text;

        if (!tab.IsDirty)
        {
            tab.IsDirty = true;
            SetTabHeader(selected, tab.Name, tabId, true);
        }
    }

    private void SavedScriptsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedScriptsList.SelectedItem is not string selectedItem)
        {
            return;
        }

        if (selectedItem.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            var existing = _tabs.Values.FirstOrDefault(t => t.Name.Equals(selectedItem, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                ScriptTabs.SelectedItem = _tabViews[existing.Id];
                StatusText.Text = $"Switched to tab: {selectedItem}";
            }
            else
            {
                CreateTabAndSelect(selectedItem, $"-- loaded local template: {selectedItem}\n");
                StatusText.Text = $"Opened script tab: {selectedItem}";
            }
        }
        else
        {
            StatusText.Text = $"Selected item: {selectedItem}";
        }
    }

    private void RunLocalButton_Click(object sender, RoutedEventArgs e)
    {
        var preview = EditorTextBox.Text.Length > 36
            ? EditorTextBox.Text[..36] + "..."
            : EditorTextBox.Text;

        StatusText.Text = $"Ran local preview at {DateTime.Now:T}: {preview.Replace(Environment.NewLine, " ")}";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var saveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SafeScriptStudio");

        Directory.CreateDirectory(saveDir);

        var fileName = string.IsNullOrWhiteSpace(ScriptNameTextBox.Text)
            ? $"script-{DateTime.Now:yyyyMMdd-HHmmss}.lua"
            : $"{SanitizeFileName(ScriptNameTextBox.Text.Trim())}.lua";

        var filePath = Path.Combine(saveDir, fileName);
        File.WriteAllText(filePath, EditorTextBox.Text);

        MarkCurrentTabSaved();
        StatusText.Text = $"Saved to {filePath}";
    }

    private void InjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureTerminalHostRunning())
        {
            StatusText.Text = "Terminal host not found. Build SafeTerminalHost and try again.";
            return;
        }

        var printValues = ExtractPrintPayloads(EditorTextBox.Text).ToList();
        if (printValues.Count == 0)
        {
            StatusText.Text = "No print(...) statements found to send.";
            return;
        }

        var sent = 0;
        foreach (var value in printValues)
        {
            if (SendToTerminal($"[LUA PRINT] {value}"))
            {
                sent++;
            }
        }

        StatusText.Text = sent > 0
            ? $"Injected to local terminal: {sent} print message(s)."
            : "Could not connect to local terminal host.";
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button closeButton || closeButton.Tag is not int tabId)
        {
            return;
        }

        CloseTabById(tabId);
    }

    private void CreateTabAndSelect(string name, string content)
    {
        var tabId = _nextTabId++;
        var tab = new ScriptTabState(tabId, name, content, false);
        var tabView = new TabItem { Tag = tabId };

        _tabs[tabId] = tab;
        _tabViews[tabId] = tabView;

        SetTabHeader(tabView, name, tabId, false);
        ScriptTabs.Items.Add(tabView);
        ScriptTabs.SelectedItem = tabView;
    }

    private void SetTabHeader(TabItem tabItem, string name, int tabId, bool isDirty)
    {
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new TextBlock
        {
            Text = "\uE943",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Margin = new Thickness(0, 0, 5, 0)
        });
        header.Children.Add(new TextBlock
        {
            Text = (string.IsNullOrWhiteSpace(name) ? "Untitled.lua" : name) + (isDirty ? " *" : string.Empty),
            MaxWidth = 160
        });
        header.Children.Add(new Button
        {
            Content = "\uE711",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 11,
            Width = 18,
            Height = 18,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.Gainsboro,
            Cursor = Cursors.Hand,
            Tag = tabId
        });

        ((Button)header.Children[2]).Click += CloseTabButton_Click;
        tabItem.Header = header;
    }

    private void SetActiveNav(Button activeButton)
    {
        var all = new[] { ScriptsButton, ConfigButton, FilesButton };

        foreach (var button in all)
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
        }

        activeButton.Background = _activeNavBackground;
        activeButton.BorderBrush = _activeNavBorder;
    }

    private void ReplaceListItems(params string[] items)
    {
        SavedScriptsList.Items.Clear();

        foreach (var item in items)
        {
            SavedScriptsList.Items.Add(item);
        }

        SavedScriptsList.SelectedIndex = -1;
    }

    private void MarkCurrentTabSaved()
    {
        if (ScriptTabs.SelectedItem is not TabItem selected || selected.Tag is not int tabId)
        {
            return;
        }

        if (!_tabs.TryGetValue(tabId, out var tab))
        {
            return;
        }

        tab.IsDirty = false;
        SetTabHeader(selected, tab.Name, tabId, false);
    }

    private void CloseCurrentTab()
    {
        if (ScriptTabs.SelectedItem is not TabItem selected || selected.Tag is not int tabId)
        {
            return;
        }

        CloseTabById(tabId);
    }

    private void CloseTabById(int tabId)
    {
        if (!_tabs.ContainsKey(tabId) || !_tabViews.ContainsKey(tabId))
        {
            return;
        }

        var wasSelected = Equals(ScriptTabs.SelectedItem, _tabViews[tabId]);
        ScriptTabs.Items.Remove(_tabViews[tabId]);
        _tabViews.Remove(tabId);
        _tabs.Remove(tabId);

        if (_tabs.Count == 0)
        {
            CreateTabAndSelect("Untitled.lua", "-- new local script\n");
            StatusText.Text = "Closed tab and created a new empty tab.";
            return;
        }

        if (wasSelected)
        {
            ScriptTabs.SelectedIndex = 0;
        }

        StatusText.Text = "Closed tab.";
    }

    private bool EnsureTerminalHostRunning()
    {
        if (_terminalHostProcess is { HasExited: false })
        {
            return true;
        }

        var baseDir = AppContext.BaseDirectory;
        var localPath = Path.Combine(baseDir, "SafeTerminalHost.exe");

        if (!File.Exists(localPath))
        {
            localPath = Path.Combine(baseDir, "SafeTerminalHost", "SafeTerminalHost.exe");
        }

        if (!File.Exists(localPath))
        {
            return false;
        }

        _terminalHostProcess = Process.Start(new ProcessStartInfo
        {
            FileName = localPath,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(localPath) ?? baseDir
        });

        return _terminalHostProcess is not null;
    }

    private static IEnumerable<string> ExtractPrintPayloads(string source)
    {
        var regex = new Regex("print\\s*\\((?<val>.+?)\\)", RegexOptions.IgnoreCase);
        foreach (Match match in regex.Matches(source))
        {
            var raw = match.Groups["val"].Value.Trim();
            if ((raw.StartsWith("\"") && raw.EndsWith("\"")) || (raw.StartsWith("'") && raw.EndsWith("'")))
            {
                raw = raw[1..^1];
            }

            if (!string.IsNullOrWhiteSpace(raw))
            {
                yield return raw;
            }
        }
    }

    private static bool SendToTerminal(string line)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", TerminalPipeName, PipeDirection.Out);
            client.Connect(700);
            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            writer.WriteLine(line);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(input.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "script" : cleaned;
    }

    private sealed class ScriptTabState(int id, string name, string content, bool isDirty)
    {
        public int Id { get; } = id;
        public string Name { get; set; } = name;
        public string Content { get; set; } = content;
        public bool IsDirty { get; set; } = isDirty;
    }
}
