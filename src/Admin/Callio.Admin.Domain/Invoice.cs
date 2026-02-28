using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Invoice : Entity<int>
{
    public int TenantId { get; private set; }
    
    public int SubscriptionId { get; private set; }
    
    public InvoiceStatus Status { get; private set; }
    
    public DateRange BillingPeriod { get; private set; }
    
    public Address BillingAddress { get; private set; }
    
    public Money Subtotal { get; private set; }
    
    public Money Tax { get; private set; }
    
    public Money Total { get; private set; }
    
    public DateTime IssuedAt { get; private set; }
    
    public DateTime? PaidAt { get; private set; }
    
    public DateTime DueDate { get; private set; }
    
    public string? ExternalPaymentRef { get; private set; }

    private readonly List<InvoiceLineItem> _lineItems = new();
    
    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    public Invoice(int tenantId, int subscriptionId, DateRange billingPeriod, Address billingAddress, DateTime dueDate, DateTime issuedAt, Money zeroAmount)
    {
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        BillingPeriod = billingPeriod;
        BillingAddress = billingAddress;
        DueDate = dueDate;
        Status = InvoiceStatus.Draft;
        IssuedAt = issuedAt;
        Subtotal = zeroAmount;
        Tax = zeroAmount;
        Total = zeroAmount;
    }

    public void AddLineItem(string description, int quantity, Money unitPrice)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot modify a non-draft invoice.");

        var item = new InvoiceLineItem(Id, description, quantity, unitPrice);
        _lineItems.Add(item);
        Recalculate();
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be issued.");
        Status = InvoiceStatus.Issued;
    }

    public void MarkPaid(string paymentRef, DateTime paidAt)
    {
        if (Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("Only issued invoices can be marked paid.");
        Status = InvoiceStatus.Paid;
        PaidAt = paidAt;
        ExternalPaymentRef = paymentRef;
    }

    public void Void()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot void a paid invoice.");
        Status = InvoiceStatus.Void;
    }

    private void Recalculate()
    {
        var currency = _lineItems.First().Total.Currency;
        var subtotal = _lineItems.Aggregate(new Money(0, currency), (acc, item) => acc.Add(item.Total));
        Subtotal = subtotal;
        // plug in your tax logic here
        Total = Subtotal.Add(Tax);
    }
}