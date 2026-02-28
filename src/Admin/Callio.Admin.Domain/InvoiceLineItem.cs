using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class InvoiceLineItem(int invoiceId, string description, int quantity, Money unitPrice)
    : Entity<int>
{
    public int InvoiceId { get; private set; } = invoiceId;
    
    public string Description { get; private set; } = description;
    
    public int Quantity { get; private set; } = quantity;
    
    public Money UnitPrice { get; private set; } = unitPrice;
    
    public Money Total { get; private set; } = unitPrice.Multiply(quantity);
}