using System;
using System.ComponentModel.DataAnnotations;

namespace BarService.Models
{
    public class DrinkTask
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsReady { get; set; }
    }
}
