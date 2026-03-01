using System;

namespace BarService.Events
{
    public class DrinkReadyEvent
    {
        public Guid DrinkTaskId { get; set; }
        public Guid OrderId { get; set; }
        public string DrinkName { get; set; } = string.Empty;
    }
}
