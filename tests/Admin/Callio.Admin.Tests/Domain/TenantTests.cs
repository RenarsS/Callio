using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.Exceptions.Tenant;
using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Exceptions;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class TenantTests
{
    private static readonly DateTime Now = new (2026, 2, 28);
    
    private const string Name = "Tenant";
    private static readonly Guid TenantCode = Guid.NewGuid();
    private static readonly Address Address = new Address("street", "postalCode", "city", "country");
    private static readonly Contact Contact = new("Jane Doe", "janedoe@gmail.com", "22334455", Address, "www.janecompany.com");
    
    private static readonly DateTime CreatedAt = new(2026, 2, 27);
    private static readonly DateTime ActivatedAt = new (2026, 2, 28);
    private static readonly DateTime DeactivatedAt = new (2026, 3, 1);

    [Fact]
    private void Tenant_AllFieldsAreValid_FieldsAreSet()
    {
        // Act
        var tenant = new Tenant(Name, TenantCode, Contact, CreatedAt, ActivatedAt, DeactivatedAt, Now)
        {
            Name = Name
        };

        // Assert
        tenant.Name.Should().Be(Name);
        tenant.TenantCode.Should().Be(TenantCode);
        tenant.CreatedAt.Should().Be(CreatedAt);
        tenant.ActivatedAt.Should().Be(ActivatedAt);
        tenant.DeactivatedAt.Should().Be(DeactivatedAt);
        tenant.Status.Value.Should().Be(Status.Enabled);
    }
    
    [Fact]
    private void Tenant_TenantCodeNotSupplied_FieldsAreSet()
    {
        // Act
        var tenant = new Tenant(Name, null, Contact, CreatedAt, ActivatedAt, DeactivatedAt, Now)
        {
            Name = Name
        };

        // Assert
        tenant.Name.Should().Be(Name);
        tenant.TenantCode.Should().NotBeNull();
        tenant.TenantCode.Should().NotBe(TenantCode);
        tenant.CreatedAt.Should().Be(CreatedAt);
        tenant.ActivatedAt.Should().Be(ActivatedAt);
        tenant.DeactivatedAt.Should().Be(DeactivatedAt);
        tenant.Status.Value.Should().Be(Status.Enabled);
    }
    
    [Fact]
    private void Tenant_NameMissing_ExceptionIsThrown()
    {
        // Act
        var act = () => new Tenant(string.Empty, TenantCode, Contact, CreatedAt, ActivatedAt, DeactivatedAt, Now)
        {
            Name = string.Empty
        };

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Name value is invalid.");
    }
    
    [Fact]
    private void Tenant_ActivatedBeforeCreated_ExceptionIsThrown()
    {
        // Act
        var act = () => new Tenant(Name, TenantCode, Contact, ActivatedAt, CreatedAt, DeactivatedAt, Now)
        {
            Name = Name
        };

        // Assert
        act.Should().Throw<InvalidDateException>().WithMessage("Tenant can't be activated before creation date.");
    }
    
    [Fact]
    private void Tenant_DeactivatedBeforeActivated_ExceptionIsThrown()
    {
        // Act
        var act = () => new Tenant(Name, TenantCode, Contact, CreatedAt, DeactivatedAt, ActivatedAt, Now)
        {
            Name = Name
        };

        // Assert
        act.Should().Throw<InvalidDateException>();
    }
}