using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [Display(Name = "Họ tên người nhận")]
    public string CustomerName { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string? CustomerPhone { get; set; }

    [Display(Name = "Địa chỉ giao hàng")]
    public string? CustomerAddress { get; set; }

    public CartViewModel Cart { get; set; } = new();
}
