using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DiscordOAuthWpf;

public partial class MainWindow : Window
{
    private readonly DiscordOAuthHandler _oauthHandler;
    private CancellationTokenSource? _loginCts;

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
            MessageBox.Show(ex.Message, "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        _loginCts?.Cancel();
        _loginCts = new CancellationTokenSource();

        try
        {
            StatusText.Text = "Starting Discord OAuth2 flow...";

            var loginUrl = _oauthHandler.CreateAuthorizationUrl();
            OAuthWebView.Source = new Uri(loginUrl);

            var code = await _oauthHandler.WaitForAuthorizationCodeAsync(_loginCts.Token);
            StatusText.Text = "Authorization code received. Exchanging token...";

            var user = await _oauthHandler.ExchangeCodeAndFetchUserAsync(code, _loginCts.Token);
            UserText.Text = $"Authenticated: {user.Username} ({user.Id})";
            StatusText.Text = "Login completed successfully.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Login cancelled.";
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

    protected override void OnClosed(EventArgs e)
    {
        _loginCts?.Cancel();
        _oauthHandler.Dispose();
        base.OnClosed(e);
    }
}
