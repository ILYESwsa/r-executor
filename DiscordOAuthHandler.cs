using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordOAuthWpf;

public sealed class DiscordOAuthHandler : IDisposable
{
    private const string DiscordApiBase = "https://discord.com/api/v10";
    private readonly OAuthConfig _config;
    private readonly HttpClient _httpClient = new();

    public DiscordOAuthHandler()
    {
        _config = OAuthConfig.LoadFromEnv();
    }

    public string CreateAuthorizationUrl()
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _config.ClientId,
            ["redirect_uri"] = _config.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "identify"
        };

        return $"https://discord.com/oauth2/authorize?{BuildQueryString(query)}";
    }

    public async Task<string> WaitForAuthorizationCodeAsync(CancellationToken cancellationToken)
    {
        var prefix = BuildHttpListenerPrefix(_config.RedirectUri);
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        try
        {
            using var registration = cancellationToken.Register(() =>
            {
                try { listener.Stop(); } catch { }
            });

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            var responseHtml = "<html><body><h3>Login complete.</h3><p>You can close this window and return to the app.</p></body></html>";
            var bytes = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            context.Response.OutputStream.Close();

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Authorization code missing in callback.");
            }

            return code;
        }
        catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        finally
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
    }

    public async Task<DiscordUser> ExchangeCodeAndFetchUserAsync(string code, CancellationToken cancellationToken)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _config.ClientId,
            ["client_secret"] = _config.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _config.RedirectUri
        };

        using var tokenResponse = await _httpClient.PostAsync(
            $"{DiscordApiBase}/oauth2/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Token exchange failed: {tokenJson}");
        }

        using var tokenDoc = JsonDocument.Parse(tokenJson);
        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new InvalidOperationException("access_token not present in Discord token response.");
        }

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Received empty access token from Discord.");
        }

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, $"{DiscordApiBase}/users/@me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var meResponse = await _httpClient.SendAsync(meRequest, cancellationToken);
        var meJson = await meResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!meResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to fetch Discord user: {meJson}");
        }

        using var meDoc = JsonDocument.Parse(meJson);
        var root = meDoc.RootElement;

        return new DiscordUser(
            root.GetProperty("id").GetString() ?? string.Empty,
            root.GetProperty("username").GetString() ?? string.Empty,
            root.TryGetProperty("avatar", out var avatarElement) ? avatarElement.GetString() : null);
    }

    private static string BuildHttpListenerPrefix(string redirectUri)
    {
        var uri = new Uri(redirectUri);
        var basePath = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath : uri.AbsolutePath + "/";
        return $"{uri.Scheme}://{uri.Host}:{uri.Port}{basePath}";
    }

    private static string BuildQueryString(Dictionary<string, string> values)
    {
        var parts = new List<string>();
        foreach (var kv in values)
        {
            parts.Add($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}");
        }

        return string.Join("&", parts);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public sealed record DiscordUser(string Id, string Username, string? Avatar);

public sealed class OAuthConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
    public required string SessionSecret { get; init; }

    public static OAuthConfig LoadFromEnv()
    {
        var env = DotEnv.Load();

        return new OAuthConfig
        {
            ClientId = GetRequired("CLIENT_ID", env),
            ClientSecret = GetRequired("CLIENT_SECRET", env),
            RedirectUri = GetRequired("REDIRECT_URI", env),
            SessionSecret = GetRequired("SESSION_SECRET", env)
        };
    }

    private static string GetRequired(string key, Dictionary<string, string> env)
    {
        if (env.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var processValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(processValue))
        {
            return processValue;
        }

        throw new InvalidOperationException($"Missing required environment variable: {key}");
    }
}

public static class DotEnv
{
    public static Dictionary<string, string> Load(string fileName = ".env")
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Environment.CurrentDirectory, fileName)
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            return Parse(path);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> Parse(string path)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var index = trimmed.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }

            var key = trimmed[..index].Trim();
            var value = trimmed[(index + 1)..].Trim();
            dict[key] = value;
        }

        return dict;
    }
}
