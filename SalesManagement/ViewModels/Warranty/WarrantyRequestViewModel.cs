using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class WarrantyRequestViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số serial.")]
    [Display(Name = "Số serial")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Mô tả sự cố")]
    public string? Note { get; set; }
}
