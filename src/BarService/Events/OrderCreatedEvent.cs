using System;

namespace BarService.Events
{
    public class OrderCreatedEvent
    {
        public Guid OrderId { get; set; }
        public int TableNum { get; set; }
        public string Items { get; set; } = string.Empty;
    }
}
