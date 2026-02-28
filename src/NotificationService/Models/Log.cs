using System;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models
{
    public class Log
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}
