namespace SalesManagement.Services.Interfaces;

public interface IFileUploadService
{
    /// <summary>
    /// Saves an uploaded image under <c>wwwroot/uploads/{subFolder}</c> and returns its
    /// public URL (e.g. <c>/uploads/products/abc.jpg</c>). Returns <c>null</c> if the file
    /// is empty or not an allowed image type.
    /// </summary>
    Task<string?> SaveImageAsync(IFormFile file, string subFolder);

    /// <summary>
    /// Deletes a previously uploaded file given its public URL (e.g. <c>/uploads/products/abc.jpg</c>).
    /// </summary>
    void DeleteFile(string? publicUrl);
}
