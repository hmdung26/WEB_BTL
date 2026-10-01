using SalesManagement.Services.Interfaces;

namespace SalesManagement.Services.Implementations;

public class FileUploadService : IFileUploadService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _environment;

    public FileUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> SaveImageAsync(IFormFile file, string subFolder)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Kích thước ảnh vượt quá 5MB.");
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{fileName}";
    }

    public void DeleteFile(string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl) || !publicUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var filePath = Path.Combine(_environment.WebRootPath, publicUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
