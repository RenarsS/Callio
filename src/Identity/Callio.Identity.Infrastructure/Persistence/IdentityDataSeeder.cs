using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Callio.Identity.Infrastructure.Persistence;

public static class IdentityDataSeeder
{
    private const string SectionName = "AdminUser";
    private const string DefaultEmail = "admin@callio.local";
    private const string DefaultPassword = "ChangeMe123!";
    private const string DefaultFirstName = "Callio";
    private const string DefaultLastName = "Admin";

    public static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var section = configuration.GetSection(SectionName);
        var enabledValue = section["Enabled"];
        var enabled = !bool.TryParse(enabledValue, out var parsedEnabled) || parsedEnabled;
        if (!enabled)
        {
            logger?.LogInformation("Admin user seed is disabled.");
            return;
        }

        var email = section["Email"] ?? DefaultEmail;
        var password = section["Password"] ?? DefaultPassword;
        var firstName = section["FirstName"] ?? DefaultFirstName;
        var lastName = section["LastName"] ?? DefaultLastName;

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("AdminUser:Email must not be empty when admin user seeding is enabled.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("AdminUser:Password must not be empty when admin user seeding is enabled.");

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.Type != UserType.PowerUser)
                throw new InvalidOperationException($"A non-admin user with email '{email}' already exists.");

            if (!existing.EmailConfirmed)
            {
                existing.EmailConfirmed = true;
                var updateResult = await userManager.UpdateAsync(existing);
                EnsureSucceeded(updateResult, $"Admin user '{email}' confirmation update failed");
            }

            logger?.LogInformation("Admin user '{Email}' already exists.", email);
            return;
        }

        var admin = ApplicationUser.CreatePowerUser(email.Trim(), firstName.Trim(), lastName.Trim());
        admin.EmailConfirmed = true;

        var result = await userManager.CreateAsync(admin, password);
        EnsureSucceeded(result, $"Admin user '{email}' creation failed");

        logger?.LogInformation("Admin user '{Email}' was created.", email);
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded)
            return;

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message}: {errors}");
    }
}
