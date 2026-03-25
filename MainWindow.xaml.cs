using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiscordOAuthWpf;

public partial class MainWindow : Window
{
    private readonly DiscordOAuthHandler _oauthHandler;
    private string _currentUserId = string.Empty;

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    public MainWindow()
    {
        InitializeComponent();
        _oauthHandler = new DiscordOAuthHandler();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await OAuthWebView.EnsureCoreWebView2Async();
            StatusText.Text = "WebView2 initialized. Click 'Login with Discord' to continue.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"WebView2 initialization failed: {ex.Message}";
            MessageBox.Show(
                $"{ex.Message}\n\nPlease install Microsoft Edge WebView2 Runtime and restart the app.",
                "WebView2 Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;

        try
        {
            ShowWebViewMode();
            StatusText.Text = "Starting Discord OAuth2 flow...";

            var loginUrl = _oauthHandler.CreateAuthorizationUrl();
            OAuthWebView.Source = new Uri(loginUrl);

            var code = await _oauthHandler.WaitForAuthorizationCodeAsync(default);
            StatusText.Text = "Authorization code received. Exchanging token...";

            var user = await _oauthHandler.ExchangeCodeAndFetchUserAsync(code, default);
            ShowNativeAuthenticatedView(user);
            StatusText.Text = "Login completed. Dashboard ready. You can now apply tweaks.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Login failed: {ex.Message}";
            MessageBox.Show(ex.Message, "OAuth2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowNativeAuthenticatedView(DiscordUser user)
    {
        _currentUserId = user.Id;
        TopUserBadge.Text = user.Username;
        UserText.Text = $"Authenticated: {user.Username} ({user.Id})";
        UsernameValue.Text = $"Welcome, {user.Username}";
        UserIdValue.Text = $"Discord ID: {user.Id}";

        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            var avatarUrl = $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.png?size=128";
            AvatarImage.Source = new BitmapImage(new Uri(avatarUrl));
        }
        else
        {
            AvatarImage.Source = null;
        }

        OAuthWebView.Source = new Uri("about:blank");
        WebViewContainer.Visibility = Visibility.Collapsed;
        NativeContentContainer.Visibility = Visibility.Visible;
    }

    private void ShowWebViewMode()
    {
        NativeContentContainer.Visibility = Visibility.Collapsed;
        WebViewContainer.Visibility = Visibility.Visible;
    }

    private void ApplyTweaksButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (HighPriorityCheck.IsChecked == true)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            }
            else
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }

            if (TrimMemoryCheck.IsChecked == true)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            }

            if (ReducedMotionCheck.IsChecked == true)
            {
                RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            }
            else
            {
                RenderOptions.SetEdgeMode(this, EdgeMode.Unspecified);
            }

            StatusText.Text = "Tweaks applied successfully.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to apply tweaks: {ex.Message}";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = $"Refreshed at {DateTime.Now:T}";
    }

    private void CopyIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_currentUserId))
        {
            Clipboard.SetText(_currentUserId);
            StatusText.Text = "Discord ID copied to clipboard.";
        }
        else
        {
            StatusText.Text = "No authenticated user ID to copy.";
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        TopUserBadge.Text = "Not authenticated";
        UserText.Text = "Not authenticated";
        UsernameValue.Text = "Username: -";
        UserIdValue.Text = "Discord ID: -";
        AvatarImage.Source = null;
        _currentUserId = string.Empty;
        StatusText.Text = "Logged out. Ready for another login.";
        ShowWebViewMode();
    }

    protected override void OnClosed(EventArgs e)
    {
        _oauthHandler.Dispose();
        base.OnClosed(e);
    }
}
