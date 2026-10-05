using System;
using System.Collections.Generic;

namespace apibdmagazintehniki.Models;

public partial class ReceiptItem
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public int? VariationId { get; set; }

    public virtual ProductVariation? ProductVariation { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public virtual Receipt? Receipt { get; set; }
}
