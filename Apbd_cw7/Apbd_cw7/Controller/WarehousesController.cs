using Apbd_cw7.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Apbd_cw7.Controller;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    [HttpPost]
    [Route("api/warehouses")]
    public async Task<IActionResult> AddProducts(WarehouseProductDTO warehouseProductDto)
    {
        
    }
}