using System;

namespace OrderService.Events
{
    public class DrinkReadyEvent
    {
        public Guid DrinkTaskId { get; set; }
        public Guid OrderId { get; set; }
        public string DrinkName { get; set; } = string.Empty;
        public bool AllDrinksReady { get; set; }
    }
}
