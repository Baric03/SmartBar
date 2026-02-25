using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int TableNum { get; set; }

        [Required]
        public string Items { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending";
    }
}
