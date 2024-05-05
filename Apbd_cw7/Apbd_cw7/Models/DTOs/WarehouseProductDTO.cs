using System.ComponentModel.DataAnnotations;

namespace Apbd_cw7.Models.DTOs;

public class WarehouseProductDTO
{
    [Required]
    public int IdProduct { get; set; }
    [Required]
    public int IdWarehouse { get; set; }
    [Required]
    public int Amount { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
}