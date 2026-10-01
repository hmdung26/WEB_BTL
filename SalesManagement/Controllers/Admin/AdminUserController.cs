using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Models;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public AdminUserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.Id).ToListAsync();
        var rows = new List<AdminUserRowViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new AdminUserRowViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Phone = user.Phone,
                Address = user.Address,
                Role = roles.FirstOrDefault() ?? "User"
            });
        }

        return View(new AdminUserListViewModel { Users = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildFormAsync(new AdminUserFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Phone = model.Phone,
            Address = model.Address,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password ?? "User@123");
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["StatusMessage"] = "Tạo người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(await BuildFormAsync(model));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var model = new AdminUserFormViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            Phone = user.Phone,
            Address = user.Address,
            Role = roles.FirstOrDefault() ?? "User"
        };

        return View(await BuildFormAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminUserFormViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        user.FullName = model.FullName;
        user.Phone = model.Phone;
        user.Address = model.Address;

        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = model.Email;
            user.UserName = model.Email;
        }

        await _userManager.UpdateAsync(user);

        // Replace the user's role (single-role assignment per the permission matrix).
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        TempData["StatusMessage"] = "Cập nhật người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        // Prevent self-deletion.
        var current = await _userManager.GetUserAsync(User);
        if (current is not null && current.Id == id)
        {
            TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin && await CountAdminsAsync() <= 1)
        {
            TempData["Error"] = "Không thể xóa tài khoản admin cuối cùng.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Đã xóa người dùng.";
        }
        else
        {
            TempData["Error"] = "Không thể xóa người dùng.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminUserFormViewModel> BuildFormAsync(AdminUserFormViewModel model)
    {
        model.RoleOptions = (await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync())
            .Select(r => new SelectListItem(r.Name, r.Name!))
            .ToList();
        return model;
    }

    private async Task<int> CountAdminsAsync()
    {
        var count = 0;
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        count = admins.Count;
        return count;
    }
}
