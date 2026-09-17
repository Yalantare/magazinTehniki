using System;
using System.Collections.Generic;

namespace apibdmagazintehniki.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Role { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Email { get; set; }

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
}
