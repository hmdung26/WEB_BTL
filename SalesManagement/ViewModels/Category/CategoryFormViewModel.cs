using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SalesManagement.ViewModels;

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Danh mục cha")]
    public int? ParentId { get; set; }

    public List<SelectListItem> ParentOptions { get; set; } = new();
}
