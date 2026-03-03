using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryService.Models
{
    public class Stock
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ingredient { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }
    }
}
