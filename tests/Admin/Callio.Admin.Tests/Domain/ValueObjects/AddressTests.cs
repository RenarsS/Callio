using Callio.Admin.Domain.Exceptions.Tenant;
using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Exceptions;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain.ValueObjects;

public class AddressTests
{
    private const string Street = "Some street";
    private const string PostalCode = "4601";
    private const string City = "Some city";
    private const string Country = "Some country";
    
    [Fact]
    public void Address_AllFieldsValid_FieldsAreSet()
    {
        // Act
        var address = new Address(Street, PostalCode, City, Country);

        // Assert
        address.Street.Should().Be(Street);
        address.PostalCode.Should().Be(PostalCode);
        address.City.Should().Be(City);
        address.Country.Should().Be(Country);
    }
    
    [Fact]
    public void Address_StreetIsNotValid_ExceptionIsThrown()
    {
        // Act
        var act = () => new Address(string.Empty, PostalCode, City, Country);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Street value is invalid.");
    }
    
    [Fact]
    public void Address_PostalCodeIsNotValid_ExceptionIsThrown()
    {
        // Act
        var act = () => new Address(Street, string.Empty, City, Country);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("PostalCode value is invalid.");
    }
    
    [Fact]
    public void Address_CityIsNotValid_ExceptionIsThrown()
    {
        // Act
        var act = () => new Address(Street, PostalCode, string.Empty, Country);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("City value is invalid.");
    }
    
    [Fact]
    public void Address_CountryIsNotValid_ExceptionIsThrown()
    {
        // Act
        var act = () => new Address(Street, PostalCode, City, string.Empty);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Country value is invalid.");
    }
}