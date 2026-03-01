using System.Text.RegularExpressions;
using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Core.Domain.Validators;

namespace Callio.Admin.Domain.ValueObjects;

public partial record Contact
{
    public string Person { get; }
    
    public string Email { get; }
    
    public string Phone { get; }
    
    public Address Address { get; }
    
    public string Website { get; }
    
    private Contact() { }

    public Contact(string person, string email, string phone, Address address, string website)
    {
        if (string.IsNullOrEmpty(person))
            throw new InvalidFieldException(nameof(Person));
        
        if (!EmailRegex().IsMatch(email))
            throw new InvalidFieldException(nameof(Email));
        
        if (!PhoneRegex().IsMatch(phone))
            throw new InvalidFieldException(nameof(Phone));

        if (!WebsiteRegex().IsMatch(website))
            throw new InvalidFieldException(nameof(Website));
        
        Person = person;
        Email = email;
        Phone = phone;
        Address = address;
        Website = website;
    }

    [GeneratedRegex(RegexConstants.PhoneRegex)]
    private static partial Regex PhoneRegex();
    
    [GeneratedRegex(RegexConstants.EmailRegex)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(RegexConstants.WebsiteRegex)]
    private static partial Regex WebsiteRegex();
}