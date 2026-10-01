using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;

namespace SalesManagement.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class PaymentController : Controller
{
    private readonly AppDbContext _context;

    public PaymentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var payments = await _context.Payments
            .Include(p => p.Order)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return View(payments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, PaymentStatus status)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null)
        {
            return NotFound();
        }

        payment.Status = status;

        if (status == PaymentStatus.Paid && payment.PaidAt is null)
        {
            payment.PaidAt = DateTime.UtcNow;
        }

        if (status == PaymentStatus.Refunded && payment.PaidAt is not null)
        {
            payment.PaidAt = null;
        }

        // When payment succeeds, issue the invoice if it doesn't exist yet.
        if (status == PaymentStatus.Paid)
        {
            var hasInvoice = await _context.Invoices.AnyAsync(i => i.OrderId == payment.OrderId);
            if (!hasInvoice)
            {
                _context.Invoices.Add(new Invoice
                {
                    OrderId = payment.OrderId,
                    InvoiceNumber = GenerateInvoiceNumber(),
                    IssuedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Đã cập nhật trạng thái thanh toán của đơn #{payment.Order.Id}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AttachTransaction(int id, string transactionCode)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(transactionCode))
        {
            TempData["Error"] = "Mã giao dịch không được để trống.";
            return RedirectToAction(nameof(Index));
        }

        payment.TransactionCode = transactionCode.Trim();
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Đã gắn mã giao dịch cho đơn #{payment.Order.Id}.";
        return RedirectToAction(nameof(Index));
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000000, 99999999)}";
    }
}
