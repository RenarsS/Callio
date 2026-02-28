using Callio.Admin.Domain;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class InvoiceLineItemTests
{
    [Fact]
    public void InvoiceLineItem_FieldsAreValid_FieldsAreSet()
    {
        // Arrange
        var euro = "EUR";
        var unitPrice = new Money((decimal)3.50, euro);

        // Act
        var invoiceLineItem =  new InvoiceLineItem(1, "Services", 4, unitPrice);

        // Assert
        invoiceLineItem.InvoiceId.Should().Be(1);
        invoiceLineItem.Description.Should().Be("Services");
        invoiceLineItem.Quantity.Should().Be(4);
        invoiceLineItem.UnitPrice.Should().Be(unitPrice);
        invoiceLineItem.Total.Amount.Should().Be(14);
        invoiceLineItem.Total.Currency.Should().Be(euro);

    }
}