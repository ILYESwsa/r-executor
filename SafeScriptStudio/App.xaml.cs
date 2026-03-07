using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SafeScriptStudio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

        try
        {
            base.OnStartup(e);
            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            ReportFatal("Startup failed", ex);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("UI thread unhandled exception", e.Exception);
        MessageBox.Show(
            "Safe Script Studio hit an unexpected error. Check logs in LocalAppData\\SafeScriptStudio\\logs.",
            "Safe Script Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogError("AppDomain unhandled exception", ex);
        }
    }

    public static void ReportFatal(string context, Exception ex)
    {
        LogError(context, ex);
        MessageBox.Show(
            $"{context}.\n\n{ex.Message}\n\nA log was written to LocalAppData\\SafeScriptStudio\\logs.",
            "Safe Script Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public static void LogError(string context, Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SafeScriptStudio",
                "logs");

            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "app.log");

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:O}] {context}");
            sb.AppendLine(ex.ToString());
            sb.AppendLine(new string('-', 80));

            File.AppendAllText(logPath, sb.ToString());
        }
        catch
        {
            // best effort logging only
        }
    }
}
