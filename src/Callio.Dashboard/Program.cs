using System.Security.Claims;
using System.Text.Json;
using Callio.Admin.Components;
using Callio.Admin.Services;
using Callio.Core.Domain.Constants.Identity;
using Callio.Identity.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPolicies.DashboardAdmin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(AppClaims.UserType, UserType.PowerUser.ToString());
    });

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5043";
builder.Services.AddHttpClient("CallioApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapPost("/auth/login", SignInAdminAsync).DisableAntiforgery();
app.MapGet("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task<IResult> SignInAdminAsync(HttpContext httpContext, IHttpClientFactory httpClientFactory)
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["Email"].FirstOrDefault()?.Trim();
    var password = form["Password"].FirstOrDefault();
    var returnUrl = form["ReturnUrl"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return LoginFailure("Enter both email and password.", returnUrl);

    var api = httpClientFactory.CreateClient("CallioApi");
    using var response = await api.PostAsJsonAsync(
        "/login?useCookies=false&useSessionCookies=false",
        new AdminLoginRequest(email, password));

    if (!response.IsSuccessStatusCode)
        return LoginFailure("The email or password is incorrect.", returnUrl);

    var accessToken = await ReadAccessTokenAsync(response);
    var profile = await ReadAdminProfileAsync(api, accessToken);

    if (profile is null)
        return LoginFailure("This account does not have dashboard access.", returnUrl);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, profile.UserId),
        new(ClaimTypes.Email, profile.Email),
        new(ClaimTypes.Name, profile.DisplayName),
        new(AppClaims.DisplayName, profile.DisplayName),
        new(AppClaims.UserType, UserType.PowerUser.ToString()),
        new(AdminDashboardClaims.AccessToken, accessToken)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    return Results.Redirect(GetSafeReturnUrl(returnUrl));
}

static IResult LoginFailure(string error, string? returnUrl)
{
    var target = $"/login?error={Uri.EscapeDataString(error)}";
    if (!string.IsNullOrWhiteSpace(returnUrl))
        target += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";

    return Results.Redirect(target);
}

static string GetSafeReturnUrl(string? returnUrl)
    => !string.IsNullOrWhiteSpace(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        ? returnUrl
        : "/dashboard";

static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response)
{
    await using var stream = await response.Content.ReadAsStreamAsync();
    using var document = await JsonDocument.ParseAsync(stream);
    if (!document.RootElement.TryGetProperty("accessToken", out var tokenElement))
        throw new InvalidOperationException("The login response did not contain an access token.");

    return tokenElement.GetString() ?? throw new InvalidOperationException("The login response returned an empty access token.");
}

static async Task<AdminProfileResponse?> ReadAdminProfileAsync(HttpClient api, string accessToken)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/me");
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    using var response = await api.SendAsync(request);
    return response.IsSuccessStatusCode
        ? await response.Content.ReadFromJsonAsync<AdminProfileResponse>()
        : null;
}

internal sealed record AdminLoginRequest(string Email, string Password);

internal sealed record AdminProfileResponse(string UserId, string Email, string DisplayName);
