using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehnikiApp.Models
{
    public partial class User
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "user"; 
        public string Name { get; set; } = null!;   
        public string Phone { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Email { get; set; }
    }
}
