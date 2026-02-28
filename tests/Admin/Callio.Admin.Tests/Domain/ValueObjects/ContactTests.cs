using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Exceptions;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain.ValueObjects;

public class ContactTests
{
    private const string Person = "Jane Doe";
    private const string Email = "janedoe@gmail.com";
    private const string Phone = "22334455";
    private static readonly Address Address = new ("street", "postalCode", "city", "country");
    private const string Website = "www.janecompany.com";
    
    [Fact]
    public void Contact_AllFieldsValid_FieldsAreSet()
    {
        // Act
        var contact = new Contact(Person, Email, Phone, Address, Website);

        // Assert
        contact.Person.Should().Be(Person);
        contact.Email.Should().Be(Email);
        contact.Phone.Should().Be(Phone);
        contact.Address.Should().Be(Address);
        contact.Website.Should().Be(Website);
    }
    
    [Fact]
    public void Contact_PersonIsNotValid_ExceptionIsThrown()
    {
        // Act
        var act = () => new Contact(string.Empty, Email, Phone, Address, Website);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Person value is invalid.");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("incorrectemail")]
    [InlineData("incorrectemail@")]
    [InlineData("incorrectemail@vvvv")]
    [InlineData("@vvvv")]
    public void Contact_EmailIsNotValid_ExceptionIsThrown(string email)
    {
        // Act
        var act = () => new Contact(Person, email, Phone, Address, Website);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Email value is invalid.");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("2929292943434343")]
    [InlineData("223344h9")]
    [InlineData("#3556667")]
    public void Contact_PhoneIsNotValid_ExceptionIsThrown(string phone)
    {
        // Act
        var act = () => new Contact(Person, Email, phone, Address, Website);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Phone value is invalid.");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("543example.&^example.com")]
    public void Contact_WebsiteIsNotValid_ExceptionIsThrown(string website)
    {
        // Act
        var act = () => new Contact(Person, Email, Phone, Address, website);

        // Assert
        act.Should().Throw<InvalidFieldException>().WithMessage("Website value is invalid.");
    }
}