using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class InvoiceTests
{
    private static readonly DateRange BillingPeriod = new (new(2026, 1, 1), new(2026, 3, 1), new(2026, 3, 2));
    private static readonly  Address Address = new Address("street", "postalCode", "city", "country");
    private static readonly DateTime DueDate = new DateTime(2026, 3, 15);
    private static readonly DateTime IssuingDate = new DateTime(2026, 3, 2);
    private static readonly Money Zero = new Money(0, "EUR");
    
    
    [Fact]
    public void Invoice_AllFieldsAreValid_FieldsAreSet()
    {

        // Act
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);

        // Assert
        invoice.TenantId.Should().Be(1);
        invoice.SubscriptionId.Should().Be(1);
        invoice.BillingPeriod.Start.Should().Be(BillingPeriod.Start);
        invoice.BillingPeriod.End.Should().Be(BillingPeriod.End);
        invoice.BillingAddress.Should().Be(Address);
        invoice.DueDate.Should().Be(DueDate);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.Subtotal.Amount.Should().Be(0);
        invoice.Tax.Amount.Should().Be(0);
        invoice.Total.Amount.Should().Be(0);
    }
    
    [Fact]
    public void AddLineItem_DraftInvoice_AddedToInvoice()
    {
        // Arrange
        var euro = "EUR";
        var unitPrice = new Money((decimal)3.50, euro);
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);

        // Act
        invoice.AddLineItem("Services", 4, unitPrice);
        
        // Assert
        invoice.LineItems.Count.Should().Be(1);
        invoice.Subtotal.Amount.Should().Be(14);
        invoice.Subtotal.Currency.Should().Be(euro);
        invoice.Tax.Amount.Should().Be(0);
        invoice.Tax.Currency.Should().Be(euro);
        invoice.Total.Amount.Should().Be(14);
        invoice.Total.Currency.Should().Be(euro);
    }
    
    [Fact]
    public void AddLineItem_IssuedInvoice_ExceptionThrown()
    {
        // Arrange
        var euro = "EUR";
        var unitPrice = new Money((decimal)3.50, euro);
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);
        invoice.Issue();

        // Act
        var act = () => invoice.AddLineItem("Services", 4, unitPrice);
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot modify a non-draft invoice.");
    }
    
    [Fact]
    public void Issue_DraftInvoice_StatusChanged()
    {
        // Arrange
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);

        // Act
        invoice.Issue();  
        
        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Issued);
    }
    
    [Fact]
    public void Issue_IssuedInvoice_ExceptionThrown()
    {
        // Arrange
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);
        invoice.Issue();
        
        // Act
        var act = () => invoice.Issue();
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft invoices can be issued.");
    }
    
    [Fact]
    public void MarkPaid_IssuedInvoice_StatusChanged()
    {
        // Arrange
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);
        invoice.Issue();
        
        // Act
        invoice.MarkPaid("No.12324-09", DateTime.Now);
        
        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }
    
    [Fact]
    public void MarkPaid_DraftInvoice_ExceptionThrown()
    {
        // Arrange
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);
        
        // Act
        var act = () => invoice.MarkPaid("No.12324-09", DateTime.Now);
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Only issued invoices can be marked paid.");
    }
    
    [Fact]
    public void Void_IssuedInvoice_StatusChanged()
    {
        // Arrange
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);
        invoice.Issue();
        
        // Act
        invoice.Void();
        
        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Void);
    }
    
    [Fact]
    public void Void_DraftInvoice_ExceptionThrown()
    {
        // Arrange
        var invoice = new Invoice(1, 1, BillingPeriod, Address, DueDate, IssuingDate, Zero);
        invoice.Issue();
        invoice.MarkPaid("No.12324-09", DateTime.Now);
        
        // Act
        var act = () => invoice.Void();
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot void a paid invoice.");
    }
}