using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    public class Order
    {
        [Key]
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid Id { get; set; }

        [Required]
        public int TableNum { get; set; }

        [Required]
        public string Items { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public string Status { get; set; } = "Pending";
    }
}
