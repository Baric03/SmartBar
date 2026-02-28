using System.Threading.Tasks;
using Grpc.Core;
using InventoryService.Core.Interfaces;
using InventoryService.Protos;

namespace InventoryService.Core.Services
{
    public class InventoryGrpcService : InventoryGrpcConfig.InventoryGrpcConfigBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryGrpcService(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public override async Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
        {
            var hasEnough = await _inventoryService.HasEnoughStockAsync(request.Ingredient, request.Quantity);

            return new CheckStockResponse
            {
                HasEnoughStock = hasEnough
            };
        }

        public override async Task<CheckStockResponse> DeductStock(CheckStockRequest request, ServerCallContext context)
        {
            var success = await _inventoryService.DeductStockAsync(request.Ingredient, request.Quantity);

            return new CheckStockResponse
            {
                HasEnoughStock = success
            };
        }
    }
}
