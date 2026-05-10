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
    private const string UserTypeClaim = "callio:user_type";
    private const string TenantIdClaim = "callio:tenant_id";
    private const string DisplayNameClaim = "callio:display_name";
    private const string EmailClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
    private const string NameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
    private const string NameIdentifierClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
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
        var tokenSession = TryCreateSessionFromAccessToken(token);
        if (tokenSession is not null)
        {
            CurrentSession = tokenSession;
            return CurrentSession;
        }

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

        CurrentSession = TryCreateSessionFromAccessToken(token);
        if (CurrentSession is not null)
            return CurrentSession;

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

    private static PortalUserSession? TryCreateSessionFromAccessToken(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);
            var root = document.RootElement;

            var expiresAtUtc = ReadExpiryUtc(root);
            if (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTime.UtcNow)
                return null;

            var userId = ReadClaim(root, NameIdentifierClaim)
                         ?? ReadClaim(root, "nameid")
                         ?? ReadClaim(root, "sub");
            var email = ReadClaim(root, EmailClaim)
                        ?? ReadClaim(root, "email")
                        ?? ReadClaim(root, NameClaim)
                        ?? ReadClaim(root, "name");
            var displayName = ReadClaim(root, DisplayNameClaim)
                              ?? ReadClaim(root, NameClaim)
                              ?? ReadClaim(root, "name")
                              ?? email;
            var userType = ReadClaim(root, UserTypeClaim);
            var tenantId = ReadTenantId(root);

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(userType))
            {
                return null;
            }

            return new PortalUserSession(
                accessToken,
                userId,
                email,
                displayName,
                userType,
                tenantId,
                expiresAtUtc);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ReadExpiryUtc(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payloadBytes = Base64UrlDecode(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);
            return ReadExpiryUtc(document.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ReadExpiryUtc(JsonElement root)
    {
        if (!root.TryGetProperty("exp", out var expirationElement))
            return null;

        if (!expirationElement.TryGetInt64(out var expiration))
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(expiration).UtcDateTime;
    }

    private static int? ReadTenantId(JsonElement root)
    {
        var value = ReadClaim(root, TenantIdClaim);
        return int.TryParse(value, out var tenantId) ? tenantId : null;
    }

    private static string? ReadClaim(JsonElement root, string claimType)
    {
        if (!root.TryGetProperty(claimType, out var claimElement))
            return null;

        return claimElement.ValueKind switch
        {
            JsonValueKind.String => claimElement.GetString(),
            JsonValueKind.Number => claimElement.GetRawText(),
            JsonValueKind.Array => claimElement.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            _ => null
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var value = input.Replace('-', '+').Replace('_', '/');
        value = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
        return Convert.FromBase64String(value);
    }

    private sealed record PortalLoginRequest(string Email, string Password);
}
