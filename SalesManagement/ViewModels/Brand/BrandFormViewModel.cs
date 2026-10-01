using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class BrandFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu.")]
    [Display(Name = "Tên thương hiệu")]
    public string Name { get; set; } = null!;

    [Display(Name = "Logo (URL)")]
    public string? LogoUrl { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Tải logo lên")]
    public IFormFile? LogoFile { get; set; }
}
