using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class InvoiceLineItem : Entity<int>
{
    public int InvoiceId { get; private set; }
    
    public string Description { get; private set; }
    
    public int Quantity { get; private set; } 
    
    public Money UnitPrice { get; private set; } 
    
    public Money Total { get; private set; }

    private InvoiceLineItem () { }
    
    public InvoiceLineItem(int invoiceId, string description, int quantity, Money unitPrice)
    {
        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Total = unitPrice.Multiply(quantity);
    }
}