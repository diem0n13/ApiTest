using Microsoft.AspNetCore.Identity;

namespace Course.Models
{
    public class User : IdentityUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; } = "User";
        
        
        
        public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; }
        public  ICollection<Order> Orders { get; set; }
    }
}
