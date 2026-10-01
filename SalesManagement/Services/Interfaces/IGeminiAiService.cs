namespace SalesManagement.Services.Interfaces;

public interface IGeminiAiService
{
    Task<string> GenerateAsync(string prompt);
}
