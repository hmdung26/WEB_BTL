using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SalesManagement.ViewModels;

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Thông số kỹ thuật")]
    public string? Specifications { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Số lượng tồn kho")]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thời gian bảo hành không hợp lệ.")]
    [Display(Name = "Thời gian bảo hành (tháng)")]
    public int WarrantyPeriod { get; set; } = 12;

    [Display(Name = "Ảnh chính (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Thương hiệu")]
    public int BrandId { get; set; }

    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    public List<SelectListItem> BrandOptions { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();

    [Display(Name = "Tải ảnh chính lên")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Tải thêm ảnh sản phẩm")]
    public List<IFormFile>? ImageFiles { get; set; }

    public List<ProductImageViewModel> ExistingImages { get; set; } = new();
}
