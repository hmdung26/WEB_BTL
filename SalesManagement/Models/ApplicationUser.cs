using Microsoft.AspNetCore.Identity;

namespace SalesManagement.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
