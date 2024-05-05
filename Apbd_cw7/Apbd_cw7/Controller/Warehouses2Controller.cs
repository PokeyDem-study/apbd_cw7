using Apbd_cw7.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Apbd_cw7.Repositories;
namespace Apbd_cw7.Controller;

[ApiController]
[Route("api/[controller]")]
public class Warehouses2Controller : ControllerBase
{
    private readonly IWarehousesRepository _warehousesRepository;
    public Warehouses2Controller(IWarehousesRepository warehousesRepository)
    {
        _warehousesRepository = warehousesRepository;
    }
    
    [HttpPost]
    [Route("api/warehouses2")]
    public async Task<IActionResult> RefillProducts(WarehouseProductDTO warehouseProductDto)
    {
        if (!await _warehousesRepository.DoesProductExists(warehouseProductDto.IdProduct))
            return NotFound($"Product with id: {warehouseProductDto.IdProduct} not found");

        if (!await _warehousesRepository.DoesWarehouseExists(warehouseProductDto.IdWarehouse))
            return NotFound($"Warehouse with id: {warehouseProductDto.IdWarehouse} not found");

        if (!_warehousesRepository.CheckAmount(warehouseProductDto.Amount))
            return BadRequest("Product amount cant be 0 or less");

        if (!await _warehousesRepository.DoesOrderExists(warehouseProductDto.IdProduct, warehouseProductDto.Amount,
                warehouseProductDto.CreatedAt))
            return NotFound("Order for those parameters not found");
        
        await _warehousesRepository.RefillProducts(warehouseProductDto);

        return Ok();
    }
}