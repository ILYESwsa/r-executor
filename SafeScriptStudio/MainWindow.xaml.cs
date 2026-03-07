using System;
using System.IO;
using System.Windows;

namespace SafeScriptStudio;

public partial class MainWindow : Window
{
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

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ScriptsButton_Click(object sender, RoutedEventArgs e)
    {
        ReplaceListItems(ScriptItems);
        StatusText.Text = "Scripts view loaded.";
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ReplaceListItems(ConfigItems);
        StatusText.Text = "Config view loaded.";
    }

    private void FilesButton_Click(object sender, RoutedEventArgs e)
    {
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
        ReplaceListItems(ScriptItems);
        StatusText.Text = "All items shown.";
    }

    private void NewScriptButton_Click(object sender, RoutedEventArgs e)
    {
        ScriptNameTextBox.Text = $"SCRIPT-{DateTime.Now:HHmmss}";
        EditorTextBox.Text = "-- new local script\n";
        StatusText.Text = "Created a new local script tab.";
        EditorTextBox.Focus();
        EditorTextBox.CaretIndex = EditorTextBox.Text.Length;
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
            : $"{ScriptNameTextBox.Text.Trim().Replace(" ", "-")}.lua";

        var filePath = Path.Combine(saveDir, fileName);
        File.WriteAllText(filePath, EditorTextBox.Text);

        StatusText.Text = $"Saved to {filePath}";
    }

    private void ReplaceListItems(params string[] items)
    {
        SavedScriptsList.Items.Clear();

        foreach (var item in items)
        {
            SavedScriptsList.Items.Add(item);
        }
    }
}
