using System.Text.Json.Serialization;

namespace Course.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public decimal TotalAmount { get; set; }
        public int OrderId { get; set; }
        
        public Order Order { get; set; }
        public int ProductId { get; set; }

        public Product Product { get; set; }
    }
}
