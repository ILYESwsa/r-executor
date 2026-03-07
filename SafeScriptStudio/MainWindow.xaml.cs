using System;
using System.IO;
using System.Windows;

namespace SafeScriptStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

        var filePath = Path.Combine(saveDir, $"script-{DateTime.Now:yyyyMMdd-HHmmss}.lua");
        File.WriteAllText(filePath, EditorTextBox.Text);

        StatusText.Text = $"Saved to {filePath}";
    }
}
