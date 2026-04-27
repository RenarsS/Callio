using Microsoft.AspNetCore.Identity;

namespace Callio.Identity.Domain;

public class ApplicationUser : IdentityUser
{
    public int? TenantId { get; private set; }

    public UserType Type { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }
    
    public ApplicationUser() { }

    public static ApplicationUser CreatePowerUser(string email, string firstName, string lastName)
        => new ()
        {
            Email = email,
            UserName = email,
            FirstName =  firstName,
            LastName = lastName,
            Type =  UserType.PowerUser,
        };

    public static ApplicationUser CreateTenantUser(string email, string firstName, string lastName, int? tenantId = null)
        => new ()
        {
            Email = email,
            UserName = email,
            FirstName =  firstName,
            LastName = lastName,
            Type =  UserType.TenantUser,
            TenantId = tenantId
        };

    public static ApplicationUser CreateApplicationUser(string email, string firstName, string lastName, int tenantId)
        => CreateTenantUser(email, firstName, lastName, tenantId);

    public void LinkToTenant(int tenantId)
    {
        TenantId = tenantId;
    }

}
