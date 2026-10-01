using Microsoft.AspNetCore.Mvc.Rendering;
using SalesManagement.Models;

namespace SalesManagement.ViewModels;

public class WarehouseListViewModel
{
    public List<WarehouseItem> Items { get; set; } = new();
    public int? ProductId { get; set; }
    public string? Status { get; set; }
    public string? Keyword { get; set; }
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public int TotalCount { get; set; }
}
