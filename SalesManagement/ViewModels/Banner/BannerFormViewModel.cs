using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class BannerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = null!;

    [Display(Name = "Phụ đề")]
    public string? Subtitle { get; set; }

    [Display(Name = "Ảnh (URL)")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Đường dẫn")]
    public string? LinkUrl { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool Active { get; set; } = true;

    [Display(Name = "Thứ tự")]
    public int SortOrder { get; set; }

    [Display(Name = "Tải ảnh lên")]
    public IFormFile? ImageFile { get; set; }
}
