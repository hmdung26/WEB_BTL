using System.ComponentModel.DataAnnotations;
using SalesManagement.Models;

namespace SalesManagement.ViewModels;

public class WarrantyLookupViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số serial.")]
    [Display(Name = "Số serial")]
    public string? SerialNumber { get; set; }

    public Warranty? Warranty { get; set; }
    public WarehouseItem? WarehouseItem { get; set; }
    public List<WarrantyHistory> History { get; set; } = new();
}
