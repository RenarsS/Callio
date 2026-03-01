using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain.ValueObjects;

public record Address
{
    public string Street { get; private set; }

    public string PostalCode { get; private set; }

    public string City { get; private set; }

    public string Country { get; private set; }
    
    private Address() { }

    public Address(string street, string postalCode, string city, string country)
    {
        if (string.IsNullOrEmpty(street))
            throw new InvalidFieldException(nameof(Street));
        
        if (string.IsNullOrEmpty(postalCode))
            throw new InvalidFieldException(nameof(PostalCode));
        
        if (string.IsNullOrEmpty(city))
            throw new InvalidFieldException(nameof(City));
        
        if (string.IsNullOrEmpty(country))
            throw new InvalidFieldException(nameof(Country));
        
        Street = street;
        PostalCode = postalCode;
        City = city;
        Country = country;
    }
}