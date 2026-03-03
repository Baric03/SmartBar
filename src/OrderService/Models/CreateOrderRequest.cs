using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    /// <summary>
    /// Request model for creating a new order.
    /// Only requires the table number and items — Id and Status are assigned automatically.
    /// </summary>
    public class CreateOrderRequest
    {
        [Required]
        public int TableNum { get; set; }

        [Required]
        public string Items { get; set; } = "string";
    }
}
