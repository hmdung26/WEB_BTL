using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Services.Interfaces;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class AiController : Controller
{
    private readonly IGeminiAiService _ai;
    private readonly IReportService _reportService;
    private readonly AppDbContext _context;

    public AiController(IGeminiAiService ai, IReportService reportService, AppDbContext context)
    {
        _ai = ai;
        _reportService = reportService;
        _context = context;
    }

    // ---- Public product chatbot ----

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Chat()
    {
        return View(new AiChatViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(AiChatViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Question))
        {
            return View(model);
        }

        var prompt = await BuildChatPromptAsync(model.Question);
        var answer = await _ai.GenerateAsync(prompt);

        model.Messages = new List<ChatMessage>
        {
            new() { Role = "user", Text = model.Question },
            new() { Role = "model", Text = answer }
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return Json(new { success = false, answer = "Vui lòng nhập câu hỏi." });
        }

        var prompt = await BuildChatPromptAsync(question);
        var answer = await _ai.GenerateAsync(prompt);

        return Json(new { success = true, answer });
    }

    private async Task<string> BuildChatPromptAsync(string question)
    {
        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .OrderBy(p => p.Id)
            .Take(20)
            .ToListAsync();

        var catalog = string.Join("; ",
            products.Select(p => $"{p.Name} ({p.Brand.Name}, {p.Category.Name}) - {p.Price:N0} đ, còn {p.StockQuantity}"));

        return $@"Bạn là trợ lý tư vấn sản phẩm của một cửa hàng bán hàng. Trả lời bằng tiếng Việt, ngắn gọn, thân thiện.
Danh sách sản phẩm hiện có (tối đa 20 mục):
{catalog}

Câu hỏi của khách hàng: {question}";
    }

    // ---- Admin: AI report ----

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Report()
    {
        var prompt = await _reportService.BuildReportPromptAsync();
        var report = await _ai.GenerateAsync(prompt);
        ViewBag.Report = report;
        return View();
    }
}
