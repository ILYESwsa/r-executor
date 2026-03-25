using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DiscordOAuthWpf;

public partial class MainWindow : Window
{
    private readonly DiscordOAuthHandler _oauthHandler;

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
            StatusText.Text = "Login completed. WebView hidden; using native view.";
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
        UserText.Text = $"Authenticated: {user.Username} ({user.Id})";
        UsernameValue.Text = $"Username: {user.Username}";
        UserIdValue.Text = $"User ID: {user.Id}";

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

    private void LoginAgainButton_Click(object sender, RoutedEventArgs e)
    {
        UserText.Text = "Not authenticated";
        UsernameValue.Text = "Username: -";
        UserIdValue.Text = "User ID: -";
        AvatarImage.Source = null;
        StatusText.Text = "Ready for another login.";
        ShowWebViewMode();
    }

    protected override void OnClosed(EventArgs e)
    {
        _oauthHandler.Dispose();
        base.OnClosed(e);
    }
}
