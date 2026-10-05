using System;
using System.Collections.Generic;

namespace apibdmagazintehniki.Models;

public partial class Product
{
    public int Articul { get; set; }

    public string Title { get; set; } = null!;

    public string Manufacturer { get; set; } = null!;

    public int Category { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string? Description { get; set; }
    public string Photo { get; set; } = null!;


    public virtual Categorye? CategoryNavigation { get; set; } = null!;

    public virtual ICollection<ProductVariation> ProductVariations { get; set; } = new List<ProductVariation>();
}
