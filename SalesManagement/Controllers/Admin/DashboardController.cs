using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Services.Interfaces;

namespace SalesManagement.Controllers.Admin;

[Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly IReportService _reportService;

    public DashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _reportService.GetDashboardAsync();
        return View(model);
    }
}
