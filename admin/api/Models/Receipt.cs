using System;
using System.Collections.Generic;

namespace apibdmagazintehniki.Models;

public partial class Receipt
{
    public int ReceiptId { get; set; }

    public int UserId { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime DateTime { get; set; }

    public int Status { get; set; }

    public sbyte OrderStatus { get; set; }
    
    public string? Description { get; set; }

    public virtual ICollection<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
