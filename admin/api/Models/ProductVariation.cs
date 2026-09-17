using System.Text.Json.Serialization;

namespace apibdmagazintehniki.Models;

public class ProductVariation
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    [JsonIgnore]
    public virtual Product? Product { get; set; }
}
