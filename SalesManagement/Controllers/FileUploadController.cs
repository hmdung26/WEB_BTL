using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Services.Interfaces;

namespace SalesManagement.Controllers;

[Authorize(Roles = "Admin")]
public class FileUploadController : Controller
{
    private readonly IFileUploadService _fileUploadService;

    public FileUploadController(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile file, string subFolder = "general")
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn một tệp ảnh.";
            return View();
        }

        try
        {
            var url = await _fileUploadService.SaveImageAsync(file, subFolder);
            TempData["StatusMessage"] = $"Tải lên thành công: {url}";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return View();
    }
}
