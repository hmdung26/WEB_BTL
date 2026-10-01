using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class ReviewCreateViewModel
{
    public int ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
    public int Rating { get; set; } = 5;

    public string? Comment { get; set; }
}
