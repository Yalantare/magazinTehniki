using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace apibdmagazintehniki.Models;

public partial class Categorye
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
