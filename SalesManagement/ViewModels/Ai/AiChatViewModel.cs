namespace SalesManagement.ViewModels;

public class AiChatViewModel
{
    public string? Question { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
}
