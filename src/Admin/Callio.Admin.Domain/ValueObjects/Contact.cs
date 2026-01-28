using System.Text.RegularExpressions;
using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Core.Domain.Validators;

namespace Callio.Admin.Domain.ValueObjects;

public partial class Contact : ValueObject
{
    public string Person { get; set; }
    
    public string Email { get; set; }
    
    public string Phone { get; set; }
    
    public Address Address { get; set; }
    
    public string Website { get; set; }

    public Contact(string person, string email, string phone, Address address, string website)
    {
        if (string.IsNullOrEmpty(person))
            throw new InvalidFieldException(nameof(Person));
        
        if (!EmailRegex().IsMatch(email))
            throw new InvalidFieldException(nameof(Email));
        
        if (!PhoneRegex().IsMatch(phone))
            throw new InvalidFieldException(nameof(Phone));

        if (string.IsNullOrEmpty(website))
            throw new InvalidFieldException(nameof(Website));
        
        Person = person;
        Email =  email;
        Phone =  phone;
        Address = address;
        Website = website;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Person;
        yield return Email;
        yield return Phone;
        yield return Address;
        yield return Website;
    }

    [GeneratedRegex(RegexConstants.PhoneRegex)]
    private static partial Regex PhoneRegex();
    
    [GeneratedRegex(RegexConstants.EmailRegex)]
    private static partial Regex EmailRegex();
}