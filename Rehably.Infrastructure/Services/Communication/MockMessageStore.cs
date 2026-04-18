namespace Rehably.Infrastructure.Services.Communication;

public static class MockMessageStore
{
    private static readonly List<MockMessage> _messages = new();
    private static readonly object _lock = new();

    public static void Add(MockMessage message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    public static List<MockMessage> GetAll()
    {
        lock (_lock)
        {
            return new List<MockMessage>(_messages);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }
}

public class MockMessage
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string To { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
