using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly IProductService _productService;

    public HomeController(AppDbContext context, IProductService productService)
    {
        _context = context;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var banners = await _context.Banners
            .Where(b => b.Active)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .ToListAsync();

        var model = new HomeViewModel
        {
            Banners = banners,
            NewestProducts = await _productService.GetNewestAsync(8),
            TopRatedProducts = await _productService.GetTopRatedAsync(4),
            CategorySections = await _productService.GetProductsByCategoryAsync(4)
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
