using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Callio.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Callio.Client.Services;

public class PortalSessionService(
    HttpClient httpClient,
    TenantRequestApi tenantRequestApi,
    IJSRuntime jsRuntime,
    NavigationManager navigationManager)
{
    private const string AccessTokenStorageKey = "callio.portal.accessToken";
    private bool _initialized;

    public PortalUserSession? CurrentSession { get; private set; }

    public bool IsAuthenticated => CurrentSession is not null;

    public async Task<PortalUserSession?> EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return CurrentSession;

        _initialized = true;
        var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, AccessTokenStorageKey);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        SetAuthorizationHeader(token);
        var currentUser = await TryGetCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            await ClearAsync(cancellationToken);
            return null;
        }

        CurrentSession = CreateSession(token, currentUser);
        return CurrentSession;
    }

    public async Task<PortalUserSession?> RequireAuthenticatedAsync(
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureInitializedAsync(cancellationToken);
        if (session is not null)
            return session;

        NavigateToLogin(returnUrl);
        return null;
    }

    public async Task<PortalUserSession> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/login?useCookies=false&useSessionCookies=false",
            new PortalLoginRequest(email, password),
            cancellationToken);

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        var token = await ReadAccessTokenAsync(response, cancellationToken);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, AccessTokenStorageKey, token);
        SetAuthorizationHeader(token);
        _initialized = true;

        var currentUser = await TryGetCurrentUserAsync(cancellationToken)
                          ?? throw new InvalidOperationException("The portal user profile could not be loaded after login.");

        CurrentSession = CreateSession(token, currentUser);
        return CurrentSession;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await ClearAsync(cancellationToken);
        navigationManager.NavigateTo("/login", replace: true);
    }

    public void NavigateToLogin(string? returnUrl = null)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl)
            ? "/login"
            : $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        navigationManager.NavigateTo(target, replace: true);
    }

    private async Task<PortalCurrentUserResponse?> TryGetCurrentUserAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await tenantRequestApi.GetCurrentUserAsync(cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = null;
        CurrentSession = null;
        _initialized = true;
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, AccessTokenStorageKey);
    }

    private void SetAuthorizationHeader(string token)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("accessToken", out var tokenElement))
            throw new InvalidOperationException("The login response did not contain an access token.");

        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("The login response returned an empty access token.");

        return token;
    }

    private static PortalUserSession CreateSession(string accessToken, PortalCurrentUserResponse currentUser)
        => new(
            accessToken,
            currentUser.UserId,
            currentUser.Email,
            currentUser.DisplayName,
            currentUser.UserType,
            currentUser.TenantId,
            ReadExpiryUtc(accessToken));

    private static DateTime? ReadExpiryUtc(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);
            if (!document.RootElement.TryGetProperty("exp", out var expirationElement))
                return null;

            if (!expirationElement.TryGetInt64(out var expiration))
                return null;

            return DateTimeOffset.FromUnixTimeSeconds(expiration).UtcDateTime;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var value = input.Replace('-', '+').Replace('_', '/');
        value = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
        return Convert.FromBase64String(value);
    }

    private sealed record PortalLoginRequest(string Email, string Password);
}
