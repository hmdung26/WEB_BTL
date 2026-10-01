using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;

namespace SalesManagement.Controllers;

public class ProductReviewController : Controller
{
    private readonly AppDbContext _context;

    public ProductReviewController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int productId)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review is null)
        {
            return NotFound();
        }

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Đã xóa đánh giá.";
        return RedirectToAction("Details", "Product", new { id = productId });
    }
}
