using Apbd_cw7.Models.DTOs;

namespace Apbd_cw7.Repositories;

public interface IWarehousesRepository
{
    Task<bool> DoesProductExists(int id);
    Task<bool> DoesWarehouseExists(int id);
    Task<bool> DoesOrderExists(int id, int amount, DateTime createdAt);
    bool CheckAmount(int amount);
    Task RefillProducts(WarehouseProductDTO warehouseProductDto);

    Task RefillProductsWithProcedure(WarehouseProductDTO warehouseProductDto);
}