using System.ComponentModel.DataAnnotations;
using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class PromotionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã giảm giá.")]
    [Display(Name = "Mã giảm giá")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập tên.")]
    [Display(Name = "Tên chương trình")]
    public string Name { get; set; } = null!;

    [Display(Name = "Loại giảm giá")]
    public PromotionDiscountType DiscountType { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá trị giảm")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn tối thiểu phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giá trị đơn tối thiểu")]
    public decimal MinOrderValue { get; set; }

    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartAt { get; set; } = DateTime.Today;

    [Display(Name = "Ngày kết thúc")]
    public DateTime EndAt { get; set; } = DateTime.Today.AddMonths(1);

    [Range(0, int.MaxValue, ErrorMessage = "Giới hạn lượt dùng phải lớn hơn hoặc bằng 0.")]
    [Display(Name = "Giới hạn lượt dùng (0 = không giới hạn)")]
    public int UsageLimit { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool Active { get; set; } = true;
}
