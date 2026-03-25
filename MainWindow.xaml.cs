using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DiscordOAuthWpf;

public partial class MainWindow : Window
{
    private readonly DiscordOAuthHandler _oauthHandler;
    private readonly WindowsTweaksService _tweaksService;
    private string _currentUserId = string.Empty;
    private CancellationTokenSource? _tweakCts;

    public MainWindow()
    {
        InitializeComponent();
        _oauthHandler = new DiscordOAuthHandler();
        _tweaksService = new WindowsTweaksService();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await OAuthWebView.EnsureCoreWebView2Async();
            StatusText.Text = "WebView2 initialized. Login to unlock tweaks.";
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
            StatusText.Text = "Login completed. Tweak utility is unlocked.";
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
        UserText.Text = $"Authenticated: {user.Username}";
        UsernameValue.Text = $"Welcome, {user.Username}";
        UserIdValue.Text = $"Discord ID: {user.Id}";

        TweakListBox.ItemsSource = _tweaksService.AvailableTweaks;
        TweakListBox.SelectedItems.Clear();

        OAuthWebView.Source = new Uri("about:blank");
        WebViewContainer.Visibility = Visibility.Collapsed;
        NativeContentContainer.Visibility = Visibility.Visible;
    }

    private void ShowWebViewMode()
    {
        NativeContentContainer.Visibility = Visibility.Collapsed;
        WebViewContainer.Visibility = Visibility.Visible;
    }

    private async void ApplySelectedTweaksButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = TweakListBox.SelectedItems.Cast<TweakDefinition>().ToList();
        if (selected.Count == 0)
        {
            StatusText.Text = "Select at least one tweak to apply.";
            return;
        }

        if (selected.Any(t => t.RequiresAdmin) && !WindowsTweaksService.IsAdministrator())
        {
            StatusText.Text = "Run app as Administrator to apply selected tweaks.";
            MessageBox.Show("Selected tweaks require Administrator privileges.", "Administrator Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _tweakCts?.Cancel();
        _tweakCts = new CancellationTokenSource();

        try
        {
            StatusText.Text = "Applying selected tweaks...";
            var applied = await _tweaksService.ApplyTweaksAsync(selected, msg => Dispatcher.Invoke(() => StatusText.Text = msg), _tweakCts.Token);
            StatusText.Text = $"Applied {applied}/{selected.Count} tweaks.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Tweak operation cancelled.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to apply tweaks: {ex.Message}";
        }
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
        _currentUserId = string.Empty;
        StatusText.Text = "Logged out. Ready for another login.";
        ShowWebViewMode();
    }

    protected override void OnClosed(EventArgs e)
    {
        _tweakCts?.Cancel();
        _oauthHandler.Dispose();
        base.OnClosed(e);
    }
}
