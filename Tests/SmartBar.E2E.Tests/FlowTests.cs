using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SmartBar.E2E.Tests
{
    public class FlowTests
    {
        private readonly HttpClient _orderClient;
        private readonly HttpClient _barClient;
        private readonly HttpClient _notificationClient;
        private readonly HttpClient _inventoryClient;

        public FlowTests()
        {
            // Note: These URLs are based on the ports defined in docker-compose.yml
            _orderClient = new HttpClient { BaseAddress = new Uri("http://localhost:7224/") };
            _barClient = new HttpClient { BaseAddress = new Uri("http://localhost:7026/") };
            _notificationClient = new HttpClient { BaseAddress = new Uri("http://localhost:7081/") };
            _inventoryClient = new HttpClient { BaseAddress = new Uri("http://localhost:7042/") };
        }

        [Fact]
        public async Task Complete_Order_Preparation_Notification_Flow()
        {
            // 1. Create a unique table number for this test run
            int tableNum = new Random().Next(100, 999);
            string drinkName = "Water"; 

            Console.WriteLine($"Step 1: Placing order for {drinkName} at table {tableNum}");

            // 2. Place an Order
            var orderContent = new StringContent(
                JsonSerializer.Serialize(new { tableNum = tableNum, items = drinkName }),
                Encoding.UTF8,
                "application/json");

            var orderResponse = await _orderClient.PostAsync("api/Orders", orderContent);
            if (!orderResponse.IsSuccessStatusCode)
            {
                var error = await orderResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Order failed: {orderResponse.StatusCode} - {error}");
            }
            Assert.True(orderResponse.IsSuccessStatusCode, "Order should be created successfully.");
            
            var responseString = await orderResponse.Content.ReadAsStringAsync();
            var createdOrder = JsonSerializer.Deserialize<JsonElement>(responseString);
            Guid orderId = createdOrder.GetProperty("id").GetGuid();
            Console.WriteLine($"Step 2: Order created with ID: {orderId}");

            Console.WriteLine("Step 3: Polling Bar Service for task...");
            JsonElement? barTask = null;
            for (int i = 0; i < 15; i++)
            {
                var tasksResponse = await _barClient.GetAsync("api/Bar");
                var tasksString = await tasksResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Bar response: {tasksResponse.StatusCode} - {tasksString}");
                
                if (!tasksResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Skipping... Bar service not ready? Error: {tasksString}");
                    await Task.Delay(2000);
                    continue;
                }

                JsonElement tasks;
                try 
                {
                    tasks = JsonSerializer.Deserialize<JsonElement>(tasksString);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse JSON: {ex.Message}. Response was: {tasksString}");
                    await Task.Delay(2000);
                    continue;
                }
                
                var taskList = tasks.ValueKind == JsonValueKind.Array ? tasks : tasks.TryGetProperty("value", out var v) ? v : tasks;

                if (taskList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in taskList.EnumerateArray())
                    {
                        if (t.TryGetProperty("orderId", out var oid) && oid.GetGuid() == orderId)
                        {
                            barTask = t;
                            Console.WriteLine("Found task in Bar Service list.");
                            break;
                        }
                    }
                }
                else if (taskList.ValueKind == JsonValueKind.Object)
                {
                    if (taskList.TryGetProperty("orderId", out var oid) && oid.GetGuid() == orderId)
                    {
                        barTask = taskList;
                        Console.WriteLine("Found task in Bar Service (single object).");
                    }
                }

                if (barTask != null) break;
                Console.WriteLine($"Task not found yet. Attempt {i+1}/15. Waiting 2s...");
                await Task.Delay(2000); 
            }

            Assert.NotNull(barTask);
            Guid drinkTaskId = barTask.Value.GetProperty("id").GetGuid();

            // 4. Mark Task as Ready in Bar Service
            Console.WriteLine($"Step 4: Marking drink {drinkTaskId} as ready.");
            var readyResponse = await _barClient.PutAsync($"api/Bar/{drinkTaskId}/mark-ready", null);
            Assert.True(readyResponse.IsSuccessStatusCode, "Marking drink as ready should succeed.");

            // 5. Poll Notification Service for the completion log
            Console.WriteLine("Step 5: Polling Notification Service for completion log...");
            bool foundNotification = false;
            for (int i = 0; i < 15; i++)
            {
                var logsResponse = await _notificationClient.GetAsync("api/Notification");
                var logsData = JsonSerializer.Deserialize<JsonElement>(await logsResponse.Content.ReadAsStringAsync());
                
                var logList = logsData.ValueKind == JsonValueKind.Array ? logsData : logsData.TryGetProperty("value", out var v) ? v : logsData;

                if (logList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var log in logList.EnumerateArray())
                    {
                        string message = log.GetProperty("message").GetString() ?? "";
                        if (message.Contains(drinkName) && message.Contains(orderId.ToString()))
                        {
                            foundNotification = true;
                            Console.WriteLine("Found notification log!");
                            break;
                        }
                    }
                }

                if (foundNotification) break;
                Console.WriteLine($"Notification not found yet. Attempt {i+1}/15. Waiting 2s...");
                await Task.Delay(2000); 
            }

            Assert.True(foundNotification, "Notification for drink completion should be found.");
            Console.WriteLine("E2E Test Flow Completed Successfully!");
        }
    }
}
