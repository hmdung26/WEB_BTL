using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class WarehouseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã vạch.")]
    [Display(Name = "Mã vạch (Barcode)")]
    public string Barcode { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập số serial.")]
    [Display(Name = "Số serial")]
    public string SerialNumber { get; set; } = null!;

    [Display(Name = "Vị trí kệ")]
    public string? ShelfLocation { get; set; }

    [Display(Name = "Trạng thái")]
    public WarehouseItemStatus Status { get; set; } = WarehouseItemStatus.Available;

    [Display(Name = "Sản phẩm")]
    public int ProductId { get; set; }

    public List<SelectListItem> ProductOptions { get; set; } = new();
}
