using System;
using System.Collections.Generic;

namespace TehnikiApp.Models;

public partial class ReceiptItem
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public virtual Product Product { get; set; } = null!;

    public int? VariationId { get; set; }

    public virtual ProductVariation? ProductVariation { get; set; }

    public virtual Receipt Receipt { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int DisplayStock => ProductVariation?.Stock ?? Product?.Stock ?? 0;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal DisplayPrice => PriceAtPurchase > 0 ? PriceAtPurchase : (ProductVariation?.Price ?? Product?.Price ?? 0);
}
